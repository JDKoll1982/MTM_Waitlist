-- Stored Procedure: sp_waitlist_lugger_locations_insert
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T013)
--
-- Purpose: add one named lugger to the set, at the place in the display order the lead asked for, and record
--          that it was added (FR-003, FR-004, FR-032).
--
-- Contract:
--   IN  p_lugger_name            VARCHAR(64)   the name the lead typed
--       p_display_position       INT           where it sits in the display order
--       p_actor_employee_number  VARCHAR(32)   who added it
--       p_actor_employee_name    VARCHAR(128)  the same person's name
--
--   OUT one row, one column: affected_rows, which is 1 on a successful add.
--
-- Guarantee: the name is trimmed and a blank one is refused. A name another lugger already holds is refused,
--            compared without regard to case by the table's utf8mb4_unicode_ci collation, and the refusal is a
--            plain sentence rather than the engine's duplicate-key error. The lugger is written as `Backup`
--            with a null reason, because it holds no dump point until a scrap type's order makes it one
--            (FR-013). Every existing row at or after the requested position is shifted up by one in the same
--            transaction, so the display order stays distinct and contiguous (FR-001). One `lugger_added`
--            change row is written with both scrap-type columns null and the actor on it (FR-032).
--
-- A refused add writes nothing at all: the refusal raises before the insert, the handler rolls the
-- transaction back, and the caller sees the sentence rather than a half-written set.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_insert;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_locations_insert(
    IN p_lugger_name VARCHAR(64),
    IN p_display_position INT,
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_lugger_name VARCHAR(64) DEFAULT '';
    DECLARE v_display_position INT DEFAULT 0;
    DECLARE v_highest_position INT DEFAULT 0;
    DECLARE v_name_taken INT DEFAULT 0;
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_affected INT DEFAULT 0;
    DECLARE v_refusal VARCHAR(255) DEFAULT '';

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    -- The unique key is caught as well as checked below, so a name that slips past the check between the two
    -- statements is answered with the same plain sentence instead of a bare 1062 reaching the caller (FR-004).
    DECLARE EXIT HANDLER FOR 1062
    BEGIN
        ROLLBACK;
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A lugger with that name already exists. Choose a different name.';
    END;

    SET v_lugger_name = TRIM(COALESCE(p_lugger_name, ''));

    START TRANSACTION;

    IF v_lugger_name = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A lugger needs a name before it can be added.';
    END IF;

    -- Case-insensitive by the column collation, which is exactly the rule the unique key enforces (FR-004).
    SELECT COUNT(*)
    INTO v_name_taken
    FROM waitlist_lugger_locations l
    WHERE l.lugger_name = v_lugger_name;

    IF v_name_taken > 0 THEN
        SET v_refusal = CONCAT('A lugger called ', v_lugger_name, ' already exists. Choose a different name.');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_refusal;
    END IF;

    SELECT COALESCE(MAX(l.display_position), 0)
    INTO v_highest_position
    FROM waitlist_lugger_locations l;

    -- A position past the end is clamped to the end, otherwise "contiguous" would be broken by the call itself.
    SET v_display_position = IFNULL(p_display_position, 0);
    IF v_display_position < 1 THEN
        SET v_display_position = 1;
    END IF;
    IF v_display_position > v_highest_position + 1 THEN
        SET v_display_position = v_highest_position + 1;
    END IF;

    -- `updated_utc` is deliberately left alone: a shifted row's content did not change, only its place, and
    -- nothing in the change log records a move.
    UPDATE waitlist_lugger_locations l
    SET l.display_position = l.display_position + 1
    WHERE l.display_position >= v_display_position;

    INSERT INTO waitlist_lugger_locations (
        public_id,
        lugger_name,
        status,
        status_reason,
        display_position,
        created_utc,
        updated_utc
    )
    VALUES (
        UUID(),
        v_lugger_name,
        'Backup',
        NULL,
        v_display_position,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    );

    SET v_affected = ROW_COUNT();
    SET v_lugger_id = LAST_INSERT_ID();

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
        'lugger_added',
        v_lugger_id,
        v_lugger_name,
        NULL,
        NULL,
        NULL,
        v_lugger_name,
        NULL,
        p_actor_employee_number,
        p_actor_employee_name,
        UTC_TIMESTAMP()
    );

    COMMIT;

    SELECT v_affected AS affected_rows;
END$$

DELIMITER ;
