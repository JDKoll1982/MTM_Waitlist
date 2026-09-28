-- Stored Procedure: sp_ops_startup_logs_insert
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T048, plan D26)
--
-- Purpose: the single writer for the log store. Every entry, from every caller, arrives here.
--
-- Contract:
--   IN  p_public_id          CHAR(36)
--   IN  p_correlation_id     CHAR(36)
--   IN  p_level              VARCHAR(16)   Debug, Info, Warning, Error or Critical
--   IN  p_event_action       VARCHAR(128)  the operation under way; NOT NULL in the table, so the caller names one
--   IN  p_outcome            VARCHAR(32)   Success, Failure, Blocked or Retried
--   IN  p_actor_kind         VARCHAR(32)   NULL when nobody was acting
--   IN  p_actor_id           VARCHAR(128)  NULL when nobody was acting
--   IN  p_host_id            VARCHAR(128)  NULL when the machine could not be named
--   IN  p_mac_address        VARCHAR(64)
--   IN  p_module             VARCHAR(128) the part of the application the entry came from; an ILogger category name lands here
--   IN  p_error_type         VARCHAR(128)
--   IN  p_message            TEXT
--   IN  p_exception_detail   MEDIUMTEXT   the serialized exception chain; NULL when there was no exception
--   IN  p_error_fingerprint  CHAR(64)     NULL for an entry raised without an exception
--   IN  p_payload_json       MEDIUMTEXT   the entry's structured payload, matching the column it is written to
--   OUT the affected-row count, through the non-query seam. This is a write and returns no result set.
--
-- The chain is written here and nowhere else. The application never computes a hash and never supplies one
-- (contracts/logging-contract.md section 3), so there is exactly one implementation of the link and it cannot
-- disagree with itself.
--
-- ============================================================
-- How the chain survives concurrent writers
-- ============================================================
-- Two things have to hold at once: the entry hash has to be computed over the previous entry's hash, and two
-- writers must never read the same "previous" and both insert against it. The body does both:
--
--   1. It takes a named lock before it reads anything, so a second writer waits rather than reading a
--      predecessor that is about to change. A row lock cannot do this job on its own: on an empty table there
--      is no row to lock, and two writers each reading "no predecessor" would each write genesis and fork the
--      chain from the first entry onwards.
--   2. It does the read, the hash and the insert inside one transaction, so the row it linked to cannot change
--      under it, and the entry and its link commit together or not at all.
--
-- The lock is a mutex, not a correctness crutch: it exists because the read and the write must be one act. A
-- writer that cannot take it within the timeout is refused with `mtm_log_chain_busy` rather than being allowed
-- to guess a predecessor — the seam absorbs that refusal and drops the entry (FR-037), so a store under
-- contention loses an entry instead of corrupting the chain.
--
-- The hash covers the entry's own fields and the link to its predecessor, joined with a unit separator so that
-- two different field layouts cannot produce the same input. A NULL predecessor is the first entry's link and is
-- hashed as an empty string, which is why a stored NULL and an empty predecessor hash identically — the row is
-- still self-describing, and the first entry is the only one that can have no predecessor.
--
-- `created_utc` is read from the store clock once and used both in the hash and in the row, so the hash a
-- reader recomputes from the row always matches the one stored.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_insert;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_ops_startup_logs_insert(
    IN p_public_id CHAR(36),
    IN p_correlation_id CHAR(36),
    IN p_level VARCHAR(16),
    IN p_event_action VARCHAR(128),
    IN p_outcome VARCHAR(32),
    IN p_actor_kind VARCHAR(32),
    IN p_actor_id VARCHAR(128),
    IN p_host_id VARCHAR(128),
    IN p_mac_address VARCHAR(64),
    IN p_module VARCHAR(128),
    IN p_error_type VARCHAR(128),
    IN p_message TEXT,
    IN p_exception_detail MEDIUMTEXT,
    IN p_error_fingerprint CHAR(64),
    IN p_payload_json MEDIUMTEXT
)
BEGIN
    DECLARE v_previous_hash CHAR(64) DEFAULT NULL;
    DECLARE v_entry_hash CHAR(64) DEFAULT NULL;
    DECLARE v_created_utc DATETIME;
    DECLARE v_lock_acquired TINYINT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        IF v_lock_acquired = 1 THEN
            DO RELEASE_LOCK('mtm_waitlist.ops_startup_logs.chain');
        END IF;
        RESIGNAL;
    END;

    SET v_lock_acquired = GET_LOCK('mtm_waitlist.ops_startup_logs.chain', 10);

    IF v_lock_acquired <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'mtm_log_chain_busy';
    END IF;

    START TRANSACTION;

    SET v_created_utc = fn_server_utc_now();

    SELECT
        l.entry_hash
    INTO v_previous_hash
    FROM
        ops_startup_logs l
    ORDER BY
        l.id DESC
    LIMIT 1;

    SET v_entry_hash = SHA2(
        CONCAT_WS(
            CHAR(31),
            COALESCE(v_previous_hash, ''),
            TRIM(p_public_id),
            LOWER(TRIM(p_correlation_id)),
            v_created_utc,
            LOWER(TRIM(p_level)),
            TRIM(p_event_action),
            TRIM(p_outcome),
            COALESCE(TRIM(p_actor_kind), ''),
            COALESCE(TRIM(p_actor_id), ''),
            COALESCE(TRIM(p_host_id), ''),
            COALESCE(LOWER(TRIM(p_mac_address)), ''),
            COALESCE(TRIM(p_module), ''),
            COALESCE(TRIM(p_error_type), ''),
            TRIM(p_message),
            COALESCE(p_exception_detail, ''),
            COALESCE(LOWER(TRIM(p_error_fingerprint)), ''),
            COALESCE(p_payload_json, '')
        ),
        256
    );

    INSERT INTO ops_startup_logs (
        public_id,
        correlation_id,
        created_utc,
        level,
        event_action,
        outcome,
        actor_kind,
        actor_id,
        host_id,
        mac_address,
        module,
        error_type,
        exception_detail,
        error_fingerprint,
        message,
        payload_json,
        previous_hash,
        entry_hash
    )
    VALUES (
        TRIM(p_public_id),
        LOWER(TRIM(p_correlation_id)),
        v_created_utc,
        LOWER(TRIM(p_level)),
        TRIM(p_event_action),
        TRIM(p_outcome),
        NULLIF(TRIM(p_actor_kind), ''),
        NULLIF(TRIM(p_actor_id), ''),
        NULLIF(TRIM(p_host_id), ''),
        NULLIF(LOWER(TRIM(p_mac_address)), ''),
        NULLIF(TRIM(p_module), ''),
        NULLIF(TRIM(p_error_type), ''),
        NULLIF(p_exception_detail, ''),
        NULLIF(LOWER(TRIM(p_error_fingerprint)), ''),
        TRIM(p_message),
        NULLIF(p_payload_json, ''),
        v_previous_hash,
        v_entry_hash
    );

    COMMIT;

    IF v_lock_acquired = 1 THEN
        DO RELEASE_LOCK('mtm_waitlist.ops_startup_logs.chain');
    END IF;
END$$

DELIMITER ;
