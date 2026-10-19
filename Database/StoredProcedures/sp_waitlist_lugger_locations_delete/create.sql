-- Stored Procedure: sp_waitlist_lugger_locations_delete
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T015)
--
-- Purpose: remove one named lugger from the plant's set and leave every scrap type it served in a state the cards
--          and the editor can draw: no assignment pointing at a lugger that no longer exists, and each affected
--          scrap type still holding exactly one dump point whenever a usable lugger remains (FR-003, FR-005,
--          FR-026, FR-032).
--
-- Contract:
--   IN  p_lugger_public_id      CHAR(36)      the lugger to remove
--   IN  p_actor_employee_number VARCHAR(32)   who removed it
--   IN  p_actor_employee_name   VARCHAR(128)  the same person's name, so the notice reads without a join
--
--   OUT affected_rows INT  1 when a lugger was removed, 0 when the id matches no lugger. The result set is the
--       contract: ExecuteNonQueryAsync cannot report a count through a transaction, so the procedure answers with
--       one column named affected_rows as its last statement.
--
-- Guarantee:
--   * The lugger's name and the scrap types it takes are read BEFORE the delete, in the same transaction. A scrap
--     type whose only lugger was this one keeps no assignment row afterwards, so its key would otherwise be
--     unrecoverable and it could be neither re-derived nor recorded.
--   * The delete is the removal (research D3). The foreign key's ON DELETE CASCADE takes every assignment with it,
--     so no assignment survives the delete and none can point at a lugger that no longer exists (FR-005).
--   * Each affected scrap type is re-derived inside the same transaction, and its assignment rows are held with
--     SELECT ... FOR UPDATE for the whole of that derivation, so two leads changing the same scrap type at the
--     same moment produce one outcome rather than a half-moved arrangement.
--
--   The dump point rule, applied to one scrap type key K inside this transaction:
--     1. read K's assignment rows joined to their luggers, ordered by sort_position then lugger_name, FOR UPDATE;
--     2. renumber them contiguously from 1 so every usable lugger (Available or Backup) comes before every
--        blocked one (Full or Do Not Use), preserving each group's relative order, and update only the rows whose
--        position actually moved;
--     3. keep the existing holder when its lugger is usable, otherwise give the role to the first usable lugger in
--        the new order, otherwise leave it with nobody;
--     4. set is_dump_point on exactly that row and clear it on every other row of K;
--     5. when the holder changed, including when it changed to nobody, write one dump_point_promoted row naming
--        the lugger that held it and the one that holds it now, with a null new_value when none can receive.
--
--   * One `lugger_removed` change row is written per affected scrap type. It carries the removed name in
--     previous_value, the scrap type as spelled at the time, and a null new_value, and its lugger_id is null
--     because the lugger it names is gone. That is what keeps the notice able to say what was removed (FR-032).
--   * A lugger that took no scrap type still records one `lugger_removed` row, with both scrap_type columns null,
--     exactly as a rename of an unassigned lugger does. Without it, removing a lugger nothing was using would
--     leave no trace at all.
--   * A `dump_point_promoted` row is written wherever another lugger took the role. When the removed lugger held
--     it, previous_value names the removed lugger even though its row is already deleted, because that is the
--     lugger the handler was told about.
--   * A scrap type that loses its last lugger keeps no assignment and no dump point; its cards draw no lugger
--     cell until the lead assigns one (FR-026). The procedure never invents a replacement lugger.
--   * The change table is append only here: nothing updates or deletes a change row, and the rows survive the
--     lugger they describe.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_delete;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_locations_delete(
    IN p_lugger_public_id CHAR(36),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_affected INT DEFAULT 0;

    DECLARE v_type_total INT DEFAULT 0;
    DECLARE v_type_index INT DEFAULT 0;
    DECLARE v_key VARCHAR(128) DEFAULT '';
    DECLARE v_spelling VARCHAR(128) DEFAULT '';
    DECLARE v_was_dump_point TINYINT(1) DEFAULT 0;

    DECLARE v_row_total INT DEFAULT 0;
    DECLARE v_usable_total INT DEFAULT 0;
    DECLARE v_row_index INT DEFAULT 0;
    DECLARE v_usable_seen INT DEFAULT 0;
    DECLARE v_blocked_seen INT DEFAULT 0;
    DECLARE v_new_position INT DEFAULT 0;
    DECLARE v_assignment_id BIGINT DEFAULT NULL;
    DECLARE v_assignment_name VARCHAR(64) DEFAULT '';
    DECLARE v_assignment_status VARCHAR(16) DEFAULT '';
    DECLARE v_assignment_position INT DEFAULT 0;
    DECLARE v_assignment_is_dump_point TINYINT(1) DEFAULT 0;
    DECLARE v_assignment_usable TINYINT(1) DEFAULT 0;

    DECLARE v_holder_id BIGINT DEFAULT NULL;
    DECLARE v_holder_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_holder_usable TINYINT(1) DEFAULT 0;
    DECLARE v_first_usable_id BIGINT DEFAULT NULL;
    DECLARE v_first_usable_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_target_id BIGINT DEFAULT NULL;
    DECLARE v_target_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_previous_name VARCHAR(255) DEFAULT NULL;
    DECLARE v_change_lugger_name VARCHAR(64) DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    -- Two session-scoped staging tables, created before the transaction opens so their DDL never sits inside it.
    -- The first holds the scrap types read before the delete; the second holds one scrap type's new order, because
    -- the renumbering cannot be applied while the rows are still being read in sort_position order.
    DROP TEMPORARY TABLE IF EXISTS tmp_sp_lugger_locations_delete_types;
    CREATE TEMPORARY TABLE tmp_sp_lugger_locations_delete_types (
        scrap_type_key VARCHAR(128) NOT NULL,
        scrap_type VARCHAR(128) NOT NULL,
        is_previous_dump_point TINYINT(1) NOT NULL,
        PRIMARY KEY (scrap_type_key)
    ) ENGINE = InnoDB;

    DROP TEMPORARY TABLE IF EXISTS tmp_sp_lugger_locations_delete_order;
    CREATE TEMPORARY TABLE tmp_sp_lugger_locations_delete_order (
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
        INSERT INTO tmp_sp_lugger_locations_delete_types (scrap_type_key, scrap_type, is_previous_dump_point)
        SELECT a.scrap_type_key, MIN(a.scrap_type), MAX(a.is_dump_point)
          FROM waitlist_lugger_scrap_assignments AS a
         WHERE a.lugger_id = v_lugger_id
         GROUP BY a.scrap_type_key;

        DELETE FROM waitlist_lugger_locations
         WHERE id = v_lugger_id;

        SET v_affected = ROW_COUNT();
    END IF;

    IF v_affected > 0 THEN
        SELECT COUNT(*)
          INTO v_type_total
          FROM tmp_sp_lugger_locations_delete_types;

        SET v_type_index = 0;

        WHILE v_type_index < v_type_total DO
            SELECT scrap_type_key, scrap_type, is_previous_dump_point
              INTO v_key, v_spelling, v_was_dump_point
              FROM tmp_sp_lugger_locations_delete_types
             ORDER BY scrap_type_key
             LIMIT v_type_index, 1;

            -- Step 1: hold every remaining assignment row of this scrap type for the whole derivation.
            DELETE FROM tmp_sp_lugger_locations_delete_order;

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

            SET v_row_index = 0;
            SET v_usable_seen = 0;
            SET v_blocked_seen = 0;
            SET v_holder_id = NULL;
            SET v_holder_name = NULL;
            SET v_holder_usable = 0;
            SET v_first_usable_id = NULL;
            SET v_first_usable_name = NULL;

            -- Step 2, first half: read the rows in their current order and compute the new one into the staging
            -- table. Writing sort_position while still reading in sort_position order would make this loop revisit
            -- or skip a row.
            WHILE v_row_index < v_row_total DO
                SELECT a.id, a.is_dump_point, a.sort_position, l.lugger_name, l.status
                  INTO v_assignment_id, v_assignment_is_dump_point, v_assignment_position,
                       v_assignment_name, v_assignment_status
                  FROM waitlist_lugger_scrap_assignments AS a
                  JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
                 WHERE a.scrap_type_key = v_key
                 ORDER BY a.sort_position, l.lugger_name
                 LIMIT v_row_index, 1
                 FOR UPDATE;

                SET v_assignment_usable = IF(v_assignment_status IN ('Available', 'Backup'), 1, 0);

                IF v_assignment_usable = 1 THEN
                    SET v_new_position = v_usable_seen + 1;
                    SET v_usable_seen = v_usable_seen + 1;
                ELSE
                    SET v_new_position = v_usable_total + v_blocked_seen + 1;
                    SET v_blocked_seen = v_blocked_seen + 1;
                END IF;

                INSERT INTO tmp_sp_lugger_locations_delete_order (seq, assignment_id, new_position)
                VALUES (v_row_index + 1, v_assignment_id, v_new_position);

                IF v_assignment_is_dump_point = 1 THEN
                    SET v_holder_id = v_assignment_id;
                    SET v_holder_name = v_assignment_name;
                    SET v_holder_usable = v_assignment_usable;
                END IF;

                IF v_assignment_usable = 1 AND v_first_usable_id IS NULL THEN
                    SET v_first_usable_id = v_assignment_id;
                    SET v_first_usable_name = v_assignment_name;
                END IF;

                SET v_row_index = v_row_index + 1;
            END WHILE;

            -- Step 2, second half: only the rows whose order actually changed are written.
            UPDATE waitlist_lugger_scrap_assignments AS a
              JOIN tmp_sp_lugger_locations_delete_order AS t ON t.assignment_id = a.id
               SET a.sort_position = t.new_position,
                   a.updated_utc = UTC_TIMESTAMP()
             WHERE a.scrap_type_key = v_key
               AND a.sort_position <> t.new_position;

            -- Step 3: the current holder keeps the role while its lugger is usable; otherwise the first usable
            -- lugger in the new order takes it; otherwise nobody does.
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

                IF v_was_dump_point = 1 THEN
                    SET v_previous_name = v_lugger_name;
                ELSE
                    SET v_previous_name = v_holder_name;
                END IF;

                SET v_change_lugger_name = IFNULL(v_target_name, IFNULL(v_previous_name, v_lugger_name));

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
                    v_previous_name,
                    v_target_name,
                    NULL,
                    p_actor_employee_number,
                    p_actor_employee_name,
                    UTC_TIMESTAMP()
                );
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
            VALUES (
                UUID(),
                'lugger_removed',
                NULL,
                v_lugger_name,
                v_spelling,
                v_key,
                v_lugger_name,
                NULL,
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                UTC_TIMESTAMP()
            );

            SET v_type_index = v_type_index + 1;
        END WHILE;

        IF v_type_total = 0 THEN
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
                'lugger_removed',
                NULL,
                v_lugger_name,
                NULL,
                NULL,
                v_lugger_name,
                NULL,
                NULL,
                p_actor_employee_number,
                p_actor_employee_name,
                UTC_TIMESTAMP()
            );
        END IF;
    END IF;

    COMMIT;

    DROP TEMPORARY TABLE IF EXISTS tmp_sp_lugger_locations_delete_types;
    DROP TEMPORARY TABLE IF EXISTS tmp_sp_lugger_locations_delete_order;

    SELECT v_affected AS affected_rows;
END$$

DELIMITER ;
