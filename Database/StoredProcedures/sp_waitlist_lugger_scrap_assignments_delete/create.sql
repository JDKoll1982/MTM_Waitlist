-- Stored Procedure: sp_waitlist_lugger_scrap_assignments_delete
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T020)
--
-- Purpose: remove one lugger from one scrap type's try order and leave that scrap type complete, so a lead
--          unassigns a lugger without reconciling the dump point by hand (FR-005, FR-013).
--
-- Contract:
--   IN  p_scrap_type_key           VARCHAR(128) the normalised scrap type, LOWER(TRIM(scrap_type))
--   IN  p_lugger_public_id         CHAR(36)     the lugger to unassign
--   IN  p_actor_employee_number    VARCHAR(32)  who unassigned it
--   IN  p_actor_employee_name      VARCHAR(128) the same person's name
--
--   OUT affected_rows              1 when the pairing existed and was removed, 0 when there was no such pairing.
--
-- Guarantee:
--   One transaction. The pairing's scrap type and the lugger's name are read before the row is deleted, so the change
--   rows can name both after the row is gone. The delete then runs, and the scrap type is re-derived only when it
--   still has pairings:
--
--     1. its remaining assignment rows are held with SELECT ... FOR UPDATE for the rest of the transaction;
--     2. positions are renumbered contiguously 1..n so every usable lugger (Available or Backup) sorts before every
--        blocked one (Full or Do Not Use), each group keeping its relative order, and only rows whose position
--        changed are updated (FR-017);
--     3. the holder is the remaining is_dump_point row when its lugger is usable, otherwise the first usable lugger in
--        the new order, otherwise nobody (FR-013, FR-020).
--
--   When the removed pairing held the role and luggers remain, the first usable lugger remaining in the order takes
--   it and a dump_point_promoted row records the move, carrying the removed lugger's name as previous_value and null
--   as new_value when nothing can receive. One assignment_removed row always records the removal (previous_value = the
--   lugger's name, new_value null). When this was the last pairing for the scrap type, no re-derivation is possible:
--   the scrap type simply holds no assignment, so no role is left to move and its cards draw no lugger cell
--   (FR-026). Every row carries the actor and one shared UTC timestamp (FR-032).
--
--   The last statement is the affected_rows result set. ExecuteNonQueryAsync cannot report a count through a
--   transaction, so the result set is the contract, not a convenience.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_scrap_assignments_delete;

DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_scrap_assignments_delete(
    IN p_scrap_type_key VARCHAR(128),
    IN p_lugger_public_id CHAR(36),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_key VARCHAR(128) DEFAULT NULL;
    DECLARE v_found INT DEFAULT 0;
    DECLARE v_affected INT DEFAULT 0;
    DECLARE v_now DATETIME DEFAULT NULL;
    DECLARE v_assignment_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_scrap_type VARCHAR(128) DEFAULT NULL;
    DECLARE v_was_dump_point TINYINT DEFAULT 0;

    DECLARE v_row_count INT DEFAULT 0;
    DECLARE v_usable_count INT DEFAULT 0;
    DECLARE v_usable_seen INT DEFAULT 0;
    DECLARE v_blocked_seen INT DEFAULT 0;
    DECLARE v_new_position INT DEFAULT 0;
    DECLARE v_row_id BIGINT DEFAULT NULL;
    DECLARE v_row_position INT DEFAULT 0;
    DECLARE v_row_usable TINYINT DEFAULT 0;
    DECLARE v_row_is_dump_point TINYINT DEFAULT 0;
    DECLARE v_row_name VARCHAR(64) DEFAULT NULL;

    DECLARE v_current_holder_id BIGINT DEFAULT NULL;
    DECLARE v_current_holder_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_current_holder_usable TINYINT DEFAULT 0;
    DECLARE v_first_usable_id BIGINT DEFAULT NULL;
    DECLARE v_first_usable_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_next_holder_id BIGINT DEFAULT NULL;
    DECLARE v_next_holder_name VARCHAR(64) DEFAULT NULL;

    DECLARE v_done TINYINT DEFAULT 0;

    DECLARE cur_order CURSOR FOR
        SELECT t.assignment_id, t.sort_position, t.is_usable, t.is_dump_point, t.lugger_name
        FROM tmp_lugger_order AS t
        ORDER BY t.sort_position ASC, t.lugger_name ASC;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    SET v_key = LOWER(TRIM(IFNULL(p_scrap_type_key, '')));

    START TRANSACTION;

    SET v_now = UTC_TIMESTAMP();

    SELECT COUNT(*)
    INTO v_found
    FROM waitlist_lugger_scrap_assignments AS a
    INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
    WHERE a.scrap_type_key = v_key
      AND l.public_id = p_lugger_public_id
    FOR UPDATE;

    IF v_found = 0 THEN
        ROLLBACK;

        SELECT 0 AS affected_rows;
    ELSE
        SELECT a.id, a.lugger_id, a.scrap_type, a.is_dump_point, l.lugger_name
        INTO v_assignment_id, v_lugger_id, v_scrap_type, v_was_dump_point, v_lugger_name
        FROM waitlist_lugger_scrap_assignments AS a
        INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
        WHERE a.scrap_type_key = v_key
          AND l.public_id = p_lugger_public_id;

        DELETE FROM waitlist_lugger_scrap_assignments
        WHERE id = v_assignment_id;

        SET v_affected = ROW_COUNT();

        SELECT COUNT(*)
        INTO v_row_count
        FROM waitlist_lugger_scrap_assignments
        WHERE scrap_type_key = v_key;

        -- The role can only move when this pairing held it and the scrap type still has a lugger to give it to.
        IF v_row_count > 0 THEN
            DROP TEMPORARY TABLE IF EXISTS tmp_lugger_order;

            CREATE TEMPORARY TABLE tmp_lugger_order (
                assignment_id BIGINT NOT NULL,
                lugger_id BIGINT NOT NULL,
                lugger_name VARCHAR(64) NOT NULL,
                status VARCHAR(16) NOT NULL,
                sort_position INT NOT NULL,
                is_dump_point TINYINT(1) NOT NULL,
                is_usable TINYINT(1) NOT NULL DEFAULT 0,
                PRIMARY KEY (assignment_id)
            );

            -- The locks are taken before a single row is read for the decision, for the whole transaction.
            SELECT COUNT(*)
            INTO v_row_count
            FROM waitlist_lugger_scrap_assignments AS a
            INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
            WHERE a.scrap_type_key = v_key
            FOR UPDATE;

            INSERT INTO tmp_lugger_order (
                assignment_id,
                lugger_id,
                lugger_name,
                status,
                sort_position,
                is_dump_point
            )
            SELECT
                a.id,
                a.lugger_id,
                l.lugger_name,
                l.status,
                a.sort_position,
                a.is_dump_point
            FROM waitlist_lugger_scrap_assignments AS a
            INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
            WHERE a.scrap_type_key = v_key;

            UPDATE tmp_lugger_order
            SET is_usable = CASE WHEN status IN ('Available', 'Backup') THEN 1 ELSE 0 END;

            SELECT COUNT(*)
            INTO v_usable_count
            FROM tmp_lugger_order
            WHERE is_usable = 1;

            -- One ordered pass decides the remaining holder, the first usable lugger and every position, which also
            -- keeps a temporary table from being opened twice in one statement (MySQL 5.7 refuses that).
            SET v_current_holder_id = NULL;
            SET v_current_holder_name = NULL;
            SET v_current_holder_usable = 0;
            SET v_first_usable_id = NULL;
            SET v_first_usable_name = NULL;
            SET v_usable_seen = 0;
            SET v_blocked_seen = 0;
            SET v_done = 0;

            OPEN cur_order;

            renumber_positions: LOOP
                FETCH cur_order INTO v_row_id, v_row_position, v_row_usable, v_row_is_dump_point, v_row_name;

                IF v_done = 1 THEN
                    LEAVE renumber_positions;
                END IF;

                IF v_row_is_dump_point = 1 AND v_current_holder_id IS NULL THEN
                    SET v_current_holder_id = v_row_id;
                    SET v_current_holder_name = v_row_name;
                    SET v_current_holder_usable = v_row_usable;
                END IF;

                IF v_row_usable = 1 AND v_first_usable_id IS NULL THEN
                    SET v_first_usable_id = v_row_id;
                    SET v_first_usable_name = v_row_name;
                END IF;

                IF v_row_usable = 1 THEN
                    SET v_usable_seen = v_usable_seen + 1;
                    SET v_new_position = v_usable_seen;
                ELSE
                    SET v_blocked_seen = v_blocked_seen + 1;
                    SET v_new_position = v_usable_count + v_blocked_seen;
                END IF;

                IF v_new_position <> v_row_position THEN
                    UPDATE waitlist_lugger_scrap_assignments
                    SET sort_position = v_new_position,
                        updated_utc = v_now
                    WHERE id = v_row_id;
                END IF;
            END LOOP;

            CLOSE cur_order;

            IF v_current_holder_id IS NOT NULL AND v_current_holder_usable = 1 THEN
                SET v_next_holder_id = v_current_holder_id;
                SET v_next_holder_name = v_current_holder_name;
            ELSE
                SET v_next_holder_id = v_first_usable_id;
                SET v_next_holder_name = v_first_usable_name;
            END IF;

            UPDATE waitlist_lugger_scrap_assignments
            SET is_dump_point = 0,
                updated_utc = v_now
            WHERE scrap_type_key = v_key
              AND is_dump_point = 1
              AND (v_next_holder_id IS NULL OR id <> v_next_holder_id);

            IF v_next_holder_id IS NOT NULL THEN
                UPDATE waitlist_lugger_scrap_assignments
                SET is_dump_point = 1,
                    updated_utc = v_now
                WHERE id = v_next_holder_id
                  AND is_dump_point = 0;
            END IF;

            DROP TEMPORARY TABLE IF EXISTS tmp_lugger_order;
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
        ) VALUES (
            UUID(),
            'assignment_removed',
            v_lugger_id,
            v_lugger_name,
            v_scrap_type,
            v_key,
            v_lugger_name,
            NULL,
            NULL,
            p_actor_employee_number,
            p_actor_employee_name,
            v_now
        );

        IF v_was_dump_point = 1 AND v_row_count > 0 THEN
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
            ) VALUES (
                UUID(),
                'dump_point_promoted',
                IFNULL(v_next_holder_id, v_lugger_id),
                IFNULL(v_next_holder_name, v_lugger_name),
                v_scrap_type,
                v_key,
                v_lugger_name,
                v_next_holder_name,
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                v_now
            );
        END IF;

        COMMIT;

        SELECT v_affected AS affected_rows;
    END IF;
END
$$

DELIMITER ;
