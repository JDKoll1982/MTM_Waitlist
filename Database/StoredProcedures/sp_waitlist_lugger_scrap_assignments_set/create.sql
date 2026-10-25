-- Stored Procedure: sp_waitlist_lugger_scrap_assignments_set
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T019)
--
-- Purpose: give one scrap type one lugger at one place in that scrap type's try order, or move an existing pairing,
--          and leave the scrap type complete in the same act: contiguous positions with every usable lugger ahead
--          of every blocked one, exactly one dump point while a usable lugger can receive, and the change recorded
--          (FR-006, FR-010, FR-011, FR-013, FR-032).
--
-- Contract:
--   IN  p_scrap_type            VARCHAR(128)  the scrap type as the lead entered it
--   IN  p_lugger_public_id      CHAR(36)      the lugger to assign
--   IN  p_sort_position         INT           the position in that scrap type's try order
--   IN  p_actor_employee_number VARCHAR(32)   who assigned or reordered it
--   IN  p_actor_employee_name   VARCHAR(128)  the same person's name, so the notice reads without a join
--
--   OUT affected_rows INT  1 when a pairing was written, 0 when the lugger id matches no lugger. The result set is
--       the contract: ExecuteNonQueryAsync cannot report a count through a transaction, so the procedure answers
--       with one column named affected_rows as its last statement.
--
-- Guarantee:
--   * The key is LOWER(TRIM(p_scrap_type)) and the stored spelling is TRIM(p_scrap_type), and both are written by
--     the same statement, so the two cannot disagree (FR-009, research D5). The inner spelling is never
--     normalised: `5052 aluminum` and `5052  aluminum` stay two different scrap types.
--   * `Scrap Type Required` and `No Scrap` are refused after normalising, under any letter case and any amount of
--     surrounding space, because neither is a scrap type and neither may be assigned a lugger (FR-010). A blank
--     entry is refused as required. Nothing is written by a refusal: the signal is raised before the transaction
--     opens.
--   * Any other spelling is accepted, including one the shipped list does not name, because the setup workflow
--     accepts a scrap type it does not recognise and keeps it (FR-011).
--   * An existing pairing is updated in place, keyed on (scrap_type_key, lugger_id), which is the unique key that
--     makes one pairing exist once. Its spelling, its position and its updated_utc are rewritten; the rest of the
--     row is untouched. Otherwise the pairing is inserted with a fresh public id, is_dump_point 0 until the
--     derivation below decides, and both timestamps set to this call's clock.
--   * A position below 1 is read as 1, so a caller that sends 0 or a negative number cannot sort a lugger ahead of
--     the try order itself.
--   * The scrap type's assignment rows are held with SELECT ... FOR UPDATE for the whole derivation, so two leads
--     assigning into the same scrap type at the same moment produce one outcome rather than a half-moved order.
--
--   The dump point rule, applied to this scrap type's key K inside this transaction:
--     1. read K's assignment rows joined to their luggers, ordered by sort_position then lugger_name, FOR UPDATE;
--        the pairing this call wrote wins any tie on its own position, so the position the lead asked for is the
--        position it ends up in;
--     2. renumber them contiguously from 1 so every usable lugger (Available or Backup) comes before every
--        blocked one (Full or Do Not Use), preserving each group's relative order, and update only the rows whose
--        position actually moved. That is what makes the positions contiguous again after an assignment, and what
--        keeps the order the order the system tries them in (FR-006, FR-013);
--     3. keep the existing holder when its lugger is usable, otherwise give the role to the first usable lugger in
--        the new order, otherwise leave it with nobody;
--     4. set is_dump_point on exactly that row and clear it on every other row of K;
--     5. when the holder changed, including when it changed to nobody, write one dump_point_promoted row naming
--        the lugger that held it and the one that holds it now, with a null new_value when none can receive.
--
--   * One `assignment_added` change row is written for a new pairing, with a null previous_value and the lugger's
--     name as new_value; one `assignment_reordered` row is written for an existing pairing, naming the position it
--     held and the position it now holds. A `dump_point_promoted` row is added only when the role moved.
--   * A blocked lugger may be assigned to a scrap type and is then ordered after every usable one; it simply
--     cannot take the role. The status lives on the lugger, never on the pairing (research D2).
--   * The change table is append only here: nothing updates or deletes a change row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_scrap_assignments_set;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_scrap_assignments_set(
    IN p_scrap_type VARCHAR(128),
    IN p_lugger_public_id CHAR(36),
    IN p_sort_position INT,
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_spelling VARCHAR(128) DEFAULT '';
    DECLARE v_key VARCHAR(128) DEFAULT '';
    DECLARE v_position INT DEFAULT 1;

    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_assignment_id BIGINT DEFAULT NULL;
    DECLARE v_assignment_is_new TINYINT(1) DEFAULT 0;
    DECLARE v_existing_id BIGINT DEFAULT NULL;
    DECLARE v_existing_position INT DEFAULT NULL;
    DECLARE v_assignment_new_position INT DEFAULT NULL;
    DECLARE v_affected INT DEFAULT 0;

    DECLARE v_row_total INT DEFAULT 0;
    DECLARE v_usable_total INT DEFAULT 0;
    DECLARE v_row_index INT DEFAULT 0;
    DECLARE v_usable_seen INT DEFAULT 0;
    DECLARE v_blocked_seen INT DEFAULT 0;
    DECLARE v_new_position INT DEFAULT 0;
    DECLARE v_row_id BIGINT DEFAULT NULL;
    DECLARE v_row_name VARCHAR(64) DEFAULT '';
    DECLARE v_row_status VARCHAR(16) DEFAULT '';
    DECLARE v_row_position INT DEFAULT 0;
    DECLARE v_row_is_dump_point TINYINT(1) DEFAULT 0;
    DECLARE v_row_usable TINYINT(1) DEFAULT 0;

    DECLARE v_holder_id BIGINT DEFAULT NULL;
    DECLARE v_holder_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_holder_usable TINYINT(1) DEFAULT 0;
    DECLARE v_first_usable_id BIGINT DEFAULT NULL;
    DECLARE v_first_usable_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_target_id BIGINT DEFAULT NULL;
    DECLARE v_target_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_change_lugger_name VARCHAR(64) DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_spelling = TRIM(IFNULL(p_scrap_type, ''));
    SET v_key = LOWER(v_spelling);

    IF v_key = '' OR v_key = 'scrap type required' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Scrap Type Required is not a scrap type a lugger can be assigned to.';
    END IF;

    IF v_key = 'no scrap' THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No Scrap is not a scrap type a lugger can be assigned to.';
    END IF;

    IF p_sort_position IS NULL OR p_sort_position < 1 THEN
        SET v_position = 1;
    ELSE
        SET v_position = p_sort_position;
    END IF;

    -- One session-scoped staging table, created before the transaction opens so its DDL never sits inside it. It
    -- holds this scrap type's new order, because the renumbering cannot be applied while the rows are still being
    -- read in sort_position order.
    DROP TEMPORARY TABLE IF EXISTS tmp_sp_lugger_scrap_assignments_set_order;
    CREATE TEMPORARY TABLE tmp_sp_lugger_scrap_assignments_set_order (
        seq INT NOT NULL,
        assignment_id BIGINT NOT NULL,
        new_position INT NOT NULL,
        PRIMARY KEY (seq)
    ) ENGINE = InnoDB;

    START TRANSACTION;

    SELECT id, lugger_name
      INTO v_lugger_id, v_lugger_name
      FROM waitlist_lugger_locations
     WHERE public_id = p_lugger_public_id
     FOR UPDATE;

    IF v_lugger_id IS NOT NULL THEN
        SELECT id, sort_position
          INTO v_existing_id, v_existing_position
          FROM waitlist_lugger_scrap_assignments
         WHERE scrap_type_key = v_key
           AND lugger_id = v_lugger_id
         FOR UPDATE;

        IF v_existing_id IS NOT NULL THEN
            UPDATE waitlist_lugger_scrap_assignments
               SET scrap_type = v_spelling,
                   scrap_type_key = v_key,
                   sort_position = v_position,
                   updated_utc = UTC_TIMESTAMP()
             WHERE id = v_existing_id;

            SET v_affected = ROW_COUNT();
            SET v_assignment_id = v_existing_id;
            SET v_assignment_new_position = v_position;
        ELSE
            INSERT INTO waitlist_lugger_scrap_assignments (
                public_id,
                lugger_id,
                scrap_type,
                scrap_type_key,
                is_dump_point,
                sort_position,
                created_utc,
                updated_utc
            )
            VALUES (
                UUID(),
                v_lugger_id,
                v_spelling,
                v_key,
                0,
                v_position,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP()
            );

            SET v_affected = ROW_COUNT();
            SET v_assignment_id = LAST_INSERT_ID();
            SET v_assignment_is_new = 1;
        END IF;

        -- Step 1: hold every assignment row of this scrap type for the whole derivation.
        SELECT COUNT(*)
          INTO v_row_total
          FROM waitlist_lugger_scrap_assignments AS a
          JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
         WHERE a.scrap_type_key = v_key
         FOR UPDATE;

        SELECT COUNT(*)
          INTO v_usable_total
          FROM waitlist_lugger_scrap_assignments AS a
          JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
         WHERE a.scrap_type_key = v_key
           AND l.status IN ('Available', 'Backup');

        DELETE FROM tmp_sp_lugger_scrap_assignments_set_order;

        SET v_row_index = 0;
        SET v_usable_seen = 0;
        SET v_blocked_seen = 0;
        SET v_holder_id = NULL;
        SET v_holder_name = NULL;
        SET v_holder_usable = 0;
        SET v_first_usable_id = NULL;
        SET v_first_usable_name = NULL;

        -- Step 2, first half: read the rows in their current order and compute the new one into the staging table.
        WHILE v_row_index < v_row_total DO
            SELECT a.id, a.is_dump_point, a.sort_position, l.lugger_name, l.status
              INTO v_row_id, v_row_is_dump_point, v_row_position, v_row_name, v_row_status
              FROM waitlist_lugger_scrap_assignments AS a
              JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
             WHERE a.scrap_type_key = v_key
             ORDER BY a.sort_position, IF(a.id = v_assignment_id, 0, 1), l.lugger_name
             LIMIT v_row_index, 1
             FOR UPDATE;

            SET v_row_usable = IF(v_row_status IN ('Available', 'Backup'), 1, 0);

            IF v_row_usable = 1 THEN
                SET v_new_position = v_usable_seen + 1;
                SET v_usable_seen = v_usable_seen + 1;
            ELSE
                SET v_new_position = v_usable_total + v_blocked_seen + 1;
                SET v_blocked_seen = v_blocked_seen + 1;
            END IF;

            INSERT INTO tmp_sp_lugger_scrap_assignments_set_order (seq, assignment_id, new_position)
            VALUES (v_row_index + 1, v_row_id, v_new_position);

            IF v_row_id = v_assignment_id THEN
                SET v_assignment_new_position = v_new_position;
            END IF;

            IF v_row_is_dump_point = 1 THEN
                SET v_holder_id = v_row_id;
                SET v_holder_name = v_row_name;
                SET v_holder_usable = v_row_usable;
            END IF;

            IF v_row_usable = 1 AND v_first_usable_id IS NULL THEN
                SET v_first_usable_id = v_row_id;
                SET v_first_usable_name = v_row_name;
            END IF;

            SET v_row_index = v_row_index + 1;
        END WHILE;

        -- Step 2, second half: only the rows whose order actually changed are written.
        UPDATE waitlist_lugger_scrap_assignments AS a
          JOIN tmp_sp_lugger_scrap_assignments_set_order AS t ON t.assignment_id = a.id
           SET a.sort_position = t.new_position,
               a.updated_utc = UTC_TIMESTAMP()
         WHERE a.scrap_type_key = v_key
           AND a.sort_position <> t.new_position;

        -- Step 3: the current holder keeps the role while its lugger is usable; otherwise the first usable lugger
        -- in the new order takes it; otherwise nobody does.
        IF v_holder_id IS NOT NULL AND v_holder_usable = 1 THEN
            SET v_target_id = v_holder_id;
            SET v_target_name = v_holder_name;
        ELSE
            SET v_target_id = v_first_usable_id;
            SET v_target_name = v_first_usable_name;
        END IF;

        -- Steps 4 and 5.
        IF NOT (v_target_id <=> v_holder_id) THEN
            UPDATE waitlist_lugger_scrap_assignments
               SET is_dump_point = IF(id = v_target_id, 1, 0)
             WHERE scrap_type_key = v_key;

            SET v_change_lugger_name = IFNULL(v_target_name, IFNULL(v_holder_name, v_lugger_name));

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
                v_target_id,
                v_change_lugger_name,
                v_spelling,
                v_key,
                v_holder_name,
                v_target_name,
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                UTC_TIMESTAMP()
            );
        END IF;

        IF v_assignment_is_new = 1 THEN
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
                'assignment_added',
                v_lugger_id,
                v_lugger_name,
                v_spelling,
                v_key,
                NULL,
                v_lugger_name,
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                UTC_TIMESTAMP()
            );
        ELSE
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
                'assignment_reordered',
                v_lugger_id,
                v_lugger_name,
                v_spelling,
                v_key,
                CAST(v_existing_position AS CHAR),
                CAST(IFNULL(v_assignment_new_position, v_position) AS CHAR),
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                UTC_TIMESTAMP()
            );
        END IF;
    END IF;

    COMMIT;

    DROP TEMPORARY TABLE IF EXISTS tmp_sp_lugger_scrap_assignments_set_order;

    SELECT v_affected AS affected_rows;
END$$

DELIMITER ;
