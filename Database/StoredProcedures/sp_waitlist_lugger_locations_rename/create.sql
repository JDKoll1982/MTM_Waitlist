-- Stored Procedure: sp_waitlist_lugger_locations_rename
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T014)
--
-- Purpose: rename one lugger, refusing a blank name and a name another lugger already holds, and record the
--          rename against every scrap type the lugger takes so the in-application notice can name both what it
--          was called and what it is called now (FR-003, FR-004, FR-035).
--
-- Contract:
--   IN  p_lugger_public_id        CHAR(36)     the lugger to rename
--   IN  p_lugger_name             VARCHAR(64)  the new name
--   IN  p_actor_employee_number   VARCHAR(32)  who renamed it
--   IN  p_actor_employee_name     VARCHAR(128) the same person's name
--
--   OUT affected_rows             0 when the public id matches no lugger, 1 when it does.
--
-- Guarantee: the name is trimmed before it is judged, so a name of spaces is refused like an empty one. The
--            duplicate test runs under the same case-insensitive collation as the unique key, so `door 7 lugger`
--            is refused beside `Door 7 Lugger`, and a lugger renamed to the name it already holds is not a clash.
--            A rename writes one `lugger_renamed` change row per affected scrap type, each naming the old name in
--            previous_value and the new name in new_value, so the notice names both (FR-035). A lugger that takes
--            no scrap type writes one row with both scrap-type columns null. The unique key is still the last
--            word: a concurrent insert that beats the duplicate test is reported as a plain refusal rather than
--            escaping as a bare 1062.
--
-- One clock for the whole call: v_now_utc is read once, so the updated row and its change rows share one
-- timestamp.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_rename;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_locations_rename(
    IN p_lugger_public_id CHAR(36),
    IN p_lugger_name VARCHAR(64),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_old_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_new_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_name_holder_id BIGINT DEFAULT NULL;
    DECLARE v_affected INT DEFAULT 0;
    DECLARE v_now_utc DATETIME DEFAULT NULL;

    -- Declared ahead of the general handler so a concurrent insert that slips past the duplicate test is
    -- reported as a sentence rather than escaping as a bare 1062.
    DECLARE CONTINUE HANDLER FOR 1062
    BEGIN
        ROLLBACK;
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Another lugger already has that name.';
    END;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_new_name = TRIM(p_lugger_name);

    IF v_new_name IS NULL OR v_new_name = '' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'A lugger needs a name.';
    END IF;

    START TRANSACTION;

    SET v_now_utc = UTC_TIMESTAMP();

    SELECT
        l.id,
        l.lugger_name
    INTO v_lugger_id,
    v_old_name
    FROM
        waitlist_lugger_locations AS l
    WHERE
        l.public_id = TRIM(p_lugger_public_id)
    FOR UPDATE;

    IF v_lugger_id IS NOT NULL AND BINARY v_new_name <> BINARY v_old_name THEN
        SELECT
            l.id
        INTO v_name_holder_id
        FROM
            waitlist_lugger_locations AS l
        WHERE
            l.lugger_name = v_new_name
            AND l.id <> v_lugger_id
        LIMIT 1;

        IF v_name_holder_id IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Another lugger already has that name.';
        END IF;

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
        SELECT
            UUID(),
            'lugger_renamed',
            v_lugger_id,
            v_new_name,
            a.scrap_type,
            a.scrap_type_key,
            v_old_name,
            v_new_name,
            NULL,
            TRIM(p_actor_employee_number),
            TRIM(p_actor_employee_name),
            v_now_utc
        FROM
            waitlist_lugger_scrap_assignments AS a
        WHERE
            a.lugger_id = v_lugger_id
        ORDER BY
            a.scrap_type_key ASC,
            a.sort_position ASC;

        IF ROW_COUNT() = 0 THEN
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
                'lugger_renamed',
                v_lugger_id,
                v_new_name,
                NULL,
                NULL,
                v_old_name,
                v_new_name,
                NULL,
                TRIM(p_actor_employee_number),
                TRIM(p_actor_employee_name),
                v_now_utc
            );
        END IF;

        UPDATE waitlist_lugger_locations
        SET
            lugger_name = v_new_name,
            updated_utc = v_now_utc
        WHERE
            id = v_lugger_id;

        SET v_affected = ROW_COUNT();
    ELSEIF v_lugger_id IS NOT NULL THEN
        -- The lugger already holds this exact name. The call succeeds and changes nothing: a change row
        -- reading `renamed from X to X` would tell the reader nothing (FR-035).
        SET v_affected = 1;
    END IF;

    COMMIT;

    SELECT v_affected AS affected_rows;
END$$

DELIMITER ;
