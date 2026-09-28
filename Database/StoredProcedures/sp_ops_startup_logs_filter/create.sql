-- Stored Procedure: sp_ops_startup_logs_filter
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T049)
--
-- Purpose: the developer panel's reader. One page of entries, newest first, narrowed by whichever filters the
--          reader set.
--
-- Contract:
--   IN  p_level             VARCHAR(16)   NULL for any severity
--   IN  p_host_id           VARCHAR(128)  NULL for any machine
--   IN  p_actor_id          VARCHAR(128)  NULL for any person
--   IN  p_module            VARCHAR(128)  NULL for any module
--   IN  p_error_type        VARCHAR(128)  NULL for any fault type
--   IN  p_error_fingerprint CHAR(64)      NULL for any fingerprint
--   IN  p_from_utc          DATETIME      NULL for the start of the default window
--   IN  p_to_utc            DATETIME      NULL for now
--   IN  p_page_size         INT           NULL or not positive for the default page
--   IN  p_offset            INT           NULL or negative for the first page
--   OUT one row per entry, newest first: id, public_id, correlation_id, created_utc, level, event_action,
--       outcome, actor_kind, actor_id, host_id, mac_address, module, error_type, message, exception_detail,
--       error_fingerprint, previous_hash, entry_hash, payload_json
--
-- Both bounds are always in force. A reader that supplies neither window bound is not asking for "everything" —
-- an unbounded read of a store that only grows is the freeze this procedure exists to prevent — it is asking
-- for the default window, which is the last 30 days ending now. A page size is likewise clamped: at most 1000
-- rows, because the panel draws a table and not a report.
--
-- Filters are equality on the six panel controls and nothing else, which is why each of the six has an index
-- leading with its own column (task T047). No filter is guessed from the others: a filter the reader did not set
-- is `NULL`, and NULL means "any", distinctly from a value that happens to be the empty string.
--
-- The chain link is returned with each row. The panel shows it and a support reader can verify it, which is the
-- only way the chain is ever checked — nothing enforces it at write time beyond this procedure's own discipline.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_filter;

DELIMITER $$

CREATE PROCEDURE sp_ops_startup_logs_filter(
    IN p_level VARCHAR(16),
    IN p_host_id VARCHAR(128),
    IN p_actor_id VARCHAR(128),
    IN p_module VARCHAR(128),
    IN p_error_type VARCHAR(128),
    IN p_error_fingerprint CHAR(64),
    IN p_from_utc DATETIME,
    IN p_to_utc DATETIME,
    IN p_page_size INT,
    IN p_offset INT
)
BEGIN
    DECLARE v_page_size INT DEFAULT 200;
    DECLARE v_offset INT DEFAULT 0;
    DECLARE v_to_utc DATETIME;
    DECLARE v_from_utc DATETIME;

    SET v_page_size = CASE
        WHEN p_page_size IS NULL OR p_page_size <= 0 THEN 200
        WHEN p_page_size > 1000 THEN 1000
        ELSE p_page_size
    END;

    SET v_offset = CASE
        WHEN p_offset IS NULL OR p_offset < 0 THEN 0
        ELSE p_offset
    END;

    SET v_to_utc = COALESCE(p_to_utc, fn_server_utc_now());
    SET v_from_utc = COALESCE(p_from_utc, DATE_SUB(v_to_utc, INTERVAL 30 DAY));

    SELECT
        l.id,
        l.public_id,
        l.correlation_id,
        l.created_utc,
        l.level,
        l.event_action,
        l.outcome,
        l.actor_kind,
        l.actor_id,
        l.host_id,
        l.mac_address,
        l.module,
        l.error_type,
        l.message,
        l.exception_detail,
        l.error_fingerprint,
        l.previous_hash,
        l.entry_hash,
        l.payload_json
    FROM
        ops_startup_logs l
    WHERE
        l.created_utc >= v_from_utc
        AND l.created_utc <= v_to_utc
        AND (p_level IS NULL OR l.level = LOWER(TRIM(p_level)))
        AND (p_host_id IS NULL OR l.host_id = TRIM(p_host_id))
        AND (p_actor_id IS NULL OR l.actor_id = TRIM(p_actor_id))
        AND (p_module IS NULL OR l.module = TRIM(p_module))
        AND (p_error_type IS NULL OR l.error_type = TRIM(p_error_type))
        AND (p_error_fingerprint IS NULL OR l.error_fingerprint = LOWER(TRIM(p_error_fingerprint)))
    ORDER BY
        l.created_utc DESC,
        l.id DESC
    LIMIT v_page_size OFFSET v_offset;
END$$

DELIMITER ;
