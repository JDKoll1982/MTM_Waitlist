-- Stored Procedure: sp_waitlist_lugger_locations_promote
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T017)
--
-- Purpose: let a lead choose which assigned lugger a scrap type dumps through, and leave the scrap type in a
--          complete state: one usable holder, the previous holder stepped back to `Backup`, every blocked
--          lugger ordered after every usable one (FR-018, FR-020).
--
-- Contract:
--   IN  p_lugger_public_id           CHAR(36)      the lugger the lead chose
--       p_scrap_type_key             VARCHAR(128)  the normalised scrap type it is promoted for
--       p_actor_employee_number      VARCHAR(32)   who promoted it
--       p_actor_employee_name        VARCHAR(128)  the same person's name
--
--   OUT one row, one column: affected_rows, which is 1 when the lugger was promoted and 0 when the lugger does
--       not exist, so a caller can tell a lost race from a success.
--
-- Guarantee: the chosen lugger becomes the dump point for that scrap type and is set `Available`; the previous
--            holder returns to `Backup` unless it is the lugger being promoted (FR-018). A lugger that does not
--            take that scrap type is refused, and so is one that is `Full` or `Do Not Use`, because a blocked
--            lugger cannot receive material. The scrap type's pairing rows are held with `SELECT ... FOR UPDATE`
--            for the whole transaction and re-ordered so every usable lugger comes before every blocked one,
--            preserving each group's own order. One `dump_point_promoted` change row names both luggers, and is
--            written only when the holder actually moved.
--
-- Both refusals raise before the first write, and the exit handler rolls the transaction back, so a refused
-- promotion leaves the arrangement exactly as it was.
--
-- The write count is the promotion's own: the chosen pairing's `is_dump_point` is cleared with every other
-- pairing of the scrap type and then set, so it always changes one row and never reports a stale zero.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_promote;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_locations_promote(
    IN p_lugger_public_id CHAR(36),
    IN p_scrap_type_key VARCHAR(128),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_scrap_type_key VARCHAR(128) DEFAULT '';
    DECLARE v_scrap_type VARCHAR(128) DEFAULT NULL;
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_lugger_status VARCHAR(16) DEFAULT NULL;
    DECLARE v_assignment_count INT DEFAULT 0;
    DECLARE v_previous_holder_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_affected INT DEFAULT 0;
    DECLARE v_refusal VARCHAR(255) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    -- A temp table survives a rollback, so a stale one from an earlier refused call is cleared first.
    DROP TEMPORARY TABLE IF EXISTS tmp_waitlist_lugger_ordering;

    SET v_scrap_type_key = LOWER(TRIM(COALESCE(p_scrap_type_key, '')));

    START TRANSACTION;

    SELECT l.id, l.lugger_name, l.status
    INTO v_lugger_id, v_lugger_name, v_lugger_status
    FROM waitlist_lugger_locations l
    WHERE l.public_id = p_lugger_public_id
    FOR UPDATE;

    -- A lugger that is not there is not an error: the row may simply have been removed by another lead.
    IF v_lugger_id IS NOT NULL THEN
        SELECT COUNT(*), MIN(a.scrap_type)
        INTO v_assignment_count, v_scrap_type
        FROM waitlist_lugger_scrap_assignments a
        WHERE a.scrap_type_key = v_scrap_type_key
          AND a.lugger_id = v_lugger_id;

        IF v_assignment_count = 0 THEN
            SET v_refusal = CONCAT(v_lugger_name, ' does not take ', v_scrap_type_key, ', so it cannot hold its dump point.');
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_refusal;
        END IF;

        IF v_lugger_status IN ('Full', 'Do Not Use') THEN
            SET v_refusal = CONCAT(v_lugger_name, ' is ', v_lugger_status, ' and cannot receive material.');
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_refusal;
        END IF;

        -- Hold this scrap type's pairings for the rest of the transaction, and read the order they sit in with
        -- the contiguous re-ordering already worked out: usable first, then blocked, each group in its own order.
        CREATE TEMPORARY TABLE tmp_waitlist_lugger_ordering (
            assignment_id BIGINT NOT NULL,
            lugger_name VARCHAR(64) NOT NULL,
            is_blocked TINYINT(1) NOT NULL,
            old_position INT NOT NULL,
            new_position INT NOT NULL,
            PRIMARY KEY (assignment_id)
        );

        INSERT INTO tmp_waitlist_lugger_ordering (
            assignment_id,
            lugger_name,
            is_blocked,
            old_position,
            new_position
        )
        SELECT a.id,
               l.lugger_name,
               IF(l.status IN ('Full', 'Do Not Use'), 1, 0),
               a.sort_position,
               (SELECT COUNT(*) + 1
                FROM waitlist_lugger_scrap_assignments b
                INNER JOIN waitlist_lugger_locations bl ON bl.id = b.lugger_id
                WHERE b.scrap_type_key = a.scrap_type_key
                  AND (
                        IF(bl.status IN ('Full', 'Do Not Use'), 1, 0)
                            < IF(l.status IN ('Full', 'Do Not Use'), 1, 0)
                     OR (IF(bl.status IN ('Full', 'Do Not Use'), 1, 0)
                            = IF(l.status IN ('Full', 'Do Not Use'), 1, 0)
                         AND (b.sort_position < a.sort_position
                              OR (b.sort_position = a.sort_position
                                  AND (bl.lugger_name < l.lugger_name
                                       OR (bl.lugger_name = l.lugger_name AND b.id < a.id)))))
                  ))
        FROM waitlist_lugger_scrap_assignments a
        INNER JOIN waitlist_lugger_locations l ON l.id = a.lugger_id
        WHERE a.scrap_type_key = v_scrap_type_key
        ORDER BY a.sort_position, l.lugger_name
        FOR UPDATE;

        -- Read before anything moves: this is the holder the promotion replaces.
        SELECT l.lugger_name
        INTO v_previous_holder_name
        FROM waitlist_lugger_scrap_assignments a
        INNER JOIN waitlist_lugger_locations l ON l.id = a.lugger_id
        WHERE a.scrap_type_key = v_scrap_type_key
          AND a.is_dump_point = 1
        ORDER BY a.sort_position, l.lugger_name
        LIMIT 1;

        UPDATE waitlist_lugger_locations l
        SET l.status = 'Available',
            l.status_reason = NULL,
            l.updated_utc = UTC_TIMESTAMP()
        WHERE l.id = v_lugger_id
          AND (l.status <> 'Available' OR l.status_reason IS NOT NULL);

        UPDATE waitlist_lugger_locations l
        INNER JOIN waitlist_lugger_scrap_assignments a ON a.lugger_id = l.id
        SET l.status = 'Backup',
            l.status_reason = NULL,
            l.updated_utc = UTC_TIMESTAMP()
        WHERE a.scrap_type_key = v_scrap_type_key
          AND a.is_dump_point = 1
          AND l.id <> v_lugger_id
          AND (l.status <> 'Backup' OR l.status_reason IS NOT NULL);

        UPDATE waitlist_lugger_scrap_assignments a
        INNER JOIN tmp_waitlist_lugger_ordering o ON o.assignment_id = a.id
        SET a.sort_position = o.new_position,
            a.updated_utc = UTC_TIMESTAMP()
        WHERE a.sort_position <> o.new_position;

        -- Exactly one holder: every pairing of the scrap type is cleared, then the chosen one is set.
        UPDATE waitlist_lugger_scrap_assignments a
        SET a.is_dump_point = 0,
            a.updated_utc = UTC_TIMESTAMP()
        WHERE a.scrap_type_key = v_scrap_type_key
          AND a.is_dump_point = 1;

        UPDATE waitlist_lugger_scrap_assignments a
        SET a.is_dump_point = 1,
            a.updated_utc = UTC_TIMESTAMP()
        WHERE a.scrap_type_key = v_scrap_type_key
          AND a.lugger_id = v_lugger_id;

        SET v_affected = ROW_COUNT();

        IF v_previous_holder_name IS NULL OR v_previous_holder_name <> v_lugger_name THEN
            INSERT INTO waitlist_lugger_arrangement_changes (
                public_id,
                change_kind,
                lugger_id,
                lugger_name,
                scrap_type,
                scrap_type_key,
                previous_value,
                new_value,
                change_reason,
                changed_by_employee_number,
                changed_by_employee_name,
                changed_utc
            )
            VALUES (
                UUID(),
                'dump_point_promoted',
                v_lugger_id,
                v_lugger_name,
                v_scrap_type,
                v_scrap_type_key,
                v_previous_holder_name,
                v_lugger_name,
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                UTC_TIMESTAMP()
            );
        END IF;

        DROP TEMPORARY TABLE IF EXISTS tmp_waitlist_lugger_ordering;
    END IF;

    COMMIT;

    SELECT v_affected AS affected_rows;
END$$

DELIMITER ;
