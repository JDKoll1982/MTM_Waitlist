-- Stored Procedure: sp_waitlist_lugger_locations_status_set
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T016)
--
-- Purpose: apply one lugger's status and leave every scrap type that dumps through it complete, so a lead sets Full,
--          Backup, Available or Do Not Use and never reconciles a dump point by hand (FR-014, FR-021).
--
-- Contract:
--   IN  p_lugger_public_id         CHAR(36)     the lugger whose status is being set
--   IN  p_status                   VARCHAR(16)  one of Available, Backup, Full, Do Not Use
--   IN  p_reason                   VARCHAR(500) the reason, required for Do Not Use and cleared for the others
--   IN  p_actor_employee_number    VARCHAR(32)  who set it
--   IN  p_actor_employee_name      VARCHAR(128) the same person's name
--
--   OUT affected_rows              1 when the lugger was found and its status applied, 0 when the public id matches
--                                  no lugger.
--
-- Guarantee:
--   One transaction. A refusal raises SQLSTATE 45000 and changes nothing: a status outside the four pinned strings is
--   refused, and Do Not Use with a blank or whitespace-only reason is refused. The reason is trimmed and stored only
--   for Do Not Use; for the other three it is written as null (FR-019).
--
--   The scrap types the lugger is assigned to are read BEFORE the status changes, then the status is written, then
--   each of those scrap types is re-derived in the same transaction:
--
--     1. its assignment rows are held with SELECT ... FOR UPDATE for the rest of the transaction, which is what makes
--        two leads changing at the same moment produce one outcome rather than a half-moved arrangement;
--     2. positions are renumbered contiguously 1..n so every usable lugger (Available or Backup) sorts before every
--        blocked one (Full or Do Not Use), each group keeping its relative order, and only rows whose position
--        changed are updated (FR-017);
--     3. the holder is the current is_dump_point row when its lugger is usable, otherwise the first usable lugger in
--        the new order, otherwise nobody — so a lugger that has been emptied does not reclaim the role by itself
--        (FR-013, FR-016, FR-020);
--     4. the flag is set on exactly that row and cleared on every other row of the scrap type, so there are never two
--        dump points and never one that cannot receive while another could.
--
--   It writes one status_changed row per affected scrap type, and one dump_point_promoted row for each scrap type
--   whose role moved, naming the lugger that held it and the lugger that holds it now (null when nobody can
--   receive). A lugger assigned to no scrap type writes a single status_changed row with both scrap-type columns
--   null. Every row carries the actor and one shared UTC timestamp (FR-032).
--
--   The last statement is the affected_rows result set. ExecuteNonQueryAsync cannot report a count through a
--   transaction, so the result set is the contract, not a convenience.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_status_set;

DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_locations_status_set(
    IN p_lugger_public_id CHAR(36),
    IN p_status VARCHAR(16),
    IN p_reason VARCHAR(500),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128)
)
BEGIN
    DECLARE v_status VARCHAR(16) DEFAULT NULL;
    DECLARE v_reason VARCHAR(500) DEFAULT NULL;
    DECLARE v_old_status VARCHAR(16) DEFAULT NULL;
    DECLARE v_lugger_id BIGINT DEFAULT NULL;
    DECLARE v_lugger_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_found INT DEFAULT 0;
    DECLARE v_affected INT DEFAULT 0;
    DECLARE v_now DATETIME DEFAULT NULL;

    DECLARE v_key VARCHAR(128) DEFAULT NULL;
    DECLARE v_scrap_type VARCHAR(128) DEFAULT NULL;
    DECLARE v_affected_types INT DEFAULT 0;

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
    DECLARE v_prev_holder_id BIGINT DEFAULT NULL;
    DECLARE v_prev_holder_name VARCHAR(64) DEFAULT NULL;
    DECLARE v_next_holder_id BIGINT DEFAULT NULL;
    DECLARE v_next_holder_name VARCHAR(64) DEFAULT NULL;

    DECLARE v_done TINYINT DEFAULT 0;

    DECLARE cur_affected_types CURSOR FOR
        SELECT t.scrap_type, t.scrap_type_key
        FROM tmp_affected_lugger_types AS t
        ORDER BY t.scrap_type_key ASC;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    -- Refusals are decided before any write opens, so a rejected call leaves the arrangement untouched.
    IF p_status IS NULL
        OR BINARY p_status NOT IN (BINARY 'Available', BINARY 'Backup', BINARY 'Full', BINARY 'Do Not Use') THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'A lugger status must be Available, Backup, Full or Do Not Use.';
    END IF;

    SET v_status = p_status;

    IF v_status = 'Do Not Use' THEN
        SET v_reason = NULLIF(TRIM(IFNULL(p_reason, '')), '');
        IF v_reason IS NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'A reason is required when a lugger is marked Do Not Use.';
        END IF;
    ELSE
        SET v_reason = NULL;
    END IF;

    START TRANSACTION;

    SET v_now = UTC_TIMESTAMP();

    SELECT COUNT(*)
    INTO v_found
    FROM waitlist_lugger_locations
    WHERE public_id = p_lugger_public_id
    FOR UPDATE;

    IF v_found = 0 THEN
        ROLLBACK;

        SELECT 0 AS affected_rows;
    ELSE
        SELECT id, lugger_name, status
        INTO v_lugger_id, v_lugger_name, v_old_status
        FROM waitlist_lugger_locations
        WHERE public_id = p_lugger_public_id;

        -- The affected scrap types are read before the status moves, so the set cannot shrink underneath the loop.
        DROP TEMPORARY TABLE IF EXISTS tmp_affected_lugger_types;

        CREATE TEMPORARY TABLE tmp_affected_lugger_types (
            scrap_type VARCHAR(128) NOT NULL,
            scrap_type_key VARCHAR(128) NOT NULL,
            PRIMARY KEY (scrap_type_key)
        );

        INSERT INTO tmp_affected_lugger_types (scrap_type, scrap_type_key)
        SELECT a.scrap_type, a.scrap_type_key
        FROM waitlist_lugger_scrap_assignments AS a
        WHERE a.lugger_id = v_lugger_id;

        SELECT COUNT(*)
        INTO v_affected_types
        FROM tmp_affected_lugger_types;

        UPDATE waitlist_lugger_locations
        SET status = v_status,
            status_reason = v_reason,
            updated_utc = v_now
        WHERE id = v_lugger_id;

        -- The row matched, so the status was applied. A second write of the status it already holds reports the lugger
        -- rather than a lost race, because ROW_COUNT() answers 0 for an update that changes nothing.
        SET v_affected = ROW_COUNT();
        IF v_affected = 0 THEN
            SET v_affected = 1;
        END IF;

        SET v_done = 0;

        OPEN cur_affected_types;

        read_affected_types: LOOP
            FETCH cur_affected_types INTO v_scrap_type, v_key;

            IF v_done = 1 THEN
                LEAVE read_affected_types;
            END IF;

            -- The re-derivation owns its cursor and its own end-of-cursor flag, so reaching the end of one scrap
            -- type's lugger order cannot also end the outer loop over the affected scrap types.
            BEGIN
                DECLARE v_order_done TINYINT DEFAULT 0;
                DECLARE cur_order CURSOR FOR
                    SELECT t.assignment_id, t.sort_position, t.is_usable, t.is_dump_point, t.lugger_name
                    FROM tmp_lugger_order AS t
                    ORDER BY t.sort_position ASC, t.lugger_name ASC;
                DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_order_done = 1;

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

                -- One ordered pass decides the current holder, the first usable lugger and every position, which also
                -- keeps a temporary table from being opened twice in one statement (MySQL 5.7 refuses that).
                SET v_current_holder_id = NULL;
                SET v_current_holder_name = NULL;
                SET v_current_holder_usable = 0;
                SET v_first_usable_id = NULL;
                SET v_first_usable_name = NULL;
                SET v_usable_seen = 0;
                SET v_blocked_seen = 0;
                SET v_order_done = 0;

                OPEN cur_order;

                renumber_positions: LOOP
                    FETCH cur_order INTO v_row_id, v_row_position, v_row_usable, v_row_is_dump_point, v_row_name;

                    IF v_order_done = 1 THEN
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

                -- The holder keeps the role while its lugger can receive material; only a blocked holder is replaced.
                IF v_current_holder_id IS NOT NULL AND v_current_holder_usable = 1 THEN
                    SET v_next_holder_id = v_current_holder_id;
                    SET v_next_holder_name = v_current_holder_name;
                ELSE
                    SET v_next_holder_id = v_first_usable_id;
                    SET v_next_holder_name = v_first_usable_name;
                END IF;

                SET v_prev_holder_id = v_current_holder_id;
                SET v_prev_holder_name = v_current_holder_name;

                -- Exactly one dump point: the chosen row set, every other row of the scrap type cleared.
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
            END;

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
                'status_changed',
                v_lugger_id,
                v_lugger_name,
                v_scrap_type,
                v_key,
                v_old_status,
                v_status,
                v_reason,
                p_actor_employee_number,
                p_actor_employee_name,
                v_now
            );

            IF NOT (v_prev_holder_name <=> v_next_holder_name) THEN
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
                    IFNULL(v_next_holder_id, v_prev_holder_id),
                    IFNULL(v_next_holder_name, v_prev_holder_name),
                    v_scrap_type,
                    v_key,
                    v_prev_holder_name,
                    v_next_holder_name,
                    NULL,
                    p_actor_employee_number,
                    p_actor_employee_name,
                    v_now
                );
            END IF;
        END LOOP;

        CLOSE cur_affected_types;

        -- A lugger no scrap type dumps through still records its status change, with both scrap-type columns null.
        IF v_affected_types = 0 THEN
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
                'status_changed',
                v_lugger_id,
                v_lugger_name,
                NULL,
                NULL,
                v_old_status,
                v_status,
                v_reason,
                p_actor_employee_number,
                p_actor_employee_name,
                v_now
            );
        END IF;

        DROP TEMPORARY TABLE IF EXISTS tmp_lugger_order;
        DROP TEMPORARY TABLE IF EXISTS tmp_affected_lugger_types;

        COMMIT;

        SELECT v_affected AS affected_rows;
    END IF;
END
$$

DELIMITER ;
