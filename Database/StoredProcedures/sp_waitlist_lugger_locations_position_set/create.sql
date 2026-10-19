-- Stored Procedure: sp_waitlist_lugger_locations_position_set
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (display-order maintenance)
--
-- Purpose: move one lugger to a position in the display order and renumber the rest behind it, so a lead can
--          arrange the cards by dragging them instead of stepping a lugger up or down one place at a time
--          (FR-001, FR-003).
--
-- Contract:
--   IN  p_lugger_public_id        CHAR(36)     the lugger to move
--   IN  p_display_position        INT          1-based position it should hold
--   IN  p_actor_employee_number   VARCHAR(32)  who moved it
--   IN  p_actor_employee_name     VARCHAR(128) the same person's name
--
--   OUT affected_rows             0 when the public id matches no lugger, 1 when it does.
--
-- Guarantee: the requested position is clamped to the range the arrangement actually holds, so a move past the
--            last card lands on the last place rather than leaving a gap. The whole order is renumbered
--            contiguously from 1 in one transaction, so two luggers can never share a position and no hole is
--            left behind a move. A move that asks for the place the lugger already holds changes nothing and
--            records nothing. Any real move writes one `lugger_reordered` change row naming the old and the new
--            position, which is what keeps the audit whole (FR-032); the row carries no scrap type, because a
--            display position belongs to the lugger rather than to a pairing.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_position_set;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_locations_position_set(
    IN p_lugger_public_id CHAR(36),
    IN p_display_position INT,
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_old_position INT DEFAULT 0;
    DECLARE v_arranged_position INT DEFAULT 0;
    DECLARE v_lugger_count INT DEFAULT 0;
    DECLARE v_now_utc DATETIME DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        DROP TEMPORARY TABLE IF EXISTS tmp_lugger_placement;
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    SET v_now_utc = UTC_TIMESTAMP();

    SELECT
        l.id,
        l.lugger_name,
        l.display_position
    INTO v_lugger_id,
    v_lugger_name,
    v_old_position
    FROM
        waitlist_lugger_locations AS l
    WHERE
        l.public_id = TRIM(p_lugger_public_id)
    FOR UPDATE;

    IF v_lugger_id IS NULL THEN
        COMMIT;

        SELECT 0 AS affected_rows;
    ELSE
        SELECT
            COUNT(*)
        INTO v_lugger_count
        FROM
            waitlist_lugger_locations;

        -- The requested place, clamped to the arrangement that exists, so a drag past either end is still a move
        -- to the end rather than a refused one.
        SET v_arranged_position = GREATEST(1, LEAST(COALESCE(p_display_position, 1), v_lugger_count));

        IF v_arranged_position = v_old_position THEN
            COMMIT;

            SELECT 1 AS affected_rows;
        ELSE
            DROP TEMPORARY TABLE IF EXISTS tmp_lugger_placement;

            CREATE TEMPORARY TABLE tmp_lugger_placement (
                lugger_id BIGINT NOT NULL,
                arranged_position INT NOT NULL,
                PRIMARY KEY (lugger_id)
            );

            -- Every other lugger in its current order, numbered from 1; then the place the moved lugger takes is
            -- opened by shifting the rows at or after it down by one.
            SET @v_sequence := 0;

            INSERT INTO tmp_lugger_placement (
                lugger_id,
                arranged_position
            )
            SELECT
                l.id,
                (@v_sequence := @v_sequence + 1)
            FROM
                waitlist_lugger_locations AS l
            WHERE
                l.id <> v_lugger_id
            ORDER BY
                l.display_position ASC,
                l.lugger_name ASC;

            UPDATE tmp_lugger_placement
            SET
                arranged_position = arranged_position + 1
            WHERE
                arranged_position >= v_arranged_position;

            INSERT INTO tmp_lugger_placement (
                lugger_id,
                arranged_position
            )
            VALUES (
                v_lugger_id,
                v_arranged_position
            );

            UPDATE waitlist_lugger_locations AS l
            JOIN tmp_lugger_placement AS o ON o.lugger_id = l.id
            SET
                l.display_position = o.arranged_position,
                l.updated_utc = v_now_utc
            WHERE
                l.display_position <> o.arranged_position;

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
                'lugger_reordered',
                v_lugger_id,
                v_lugger_name,
                NULL,
                NULL,
                CAST(v_old_position AS CHAR),
                CAST(v_arranged_position AS CHAR),
                NULL,
                TRIM(p_actor_employee_number),
                TRIM(p_actor_employee_name),
                v_now_utc
            );

            DROP TEMPORARY TABLE IF EXISTS tmp_lugger_placement;

            COMMIT;

            SELECT 1 AS affected_rows;
        END IF;
    END IF;
END$$

DELIMITER ;
