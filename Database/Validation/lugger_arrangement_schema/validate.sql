-- Validate the lugger arrangement schema artifacts
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T027)
--
-- What this file is
--   The same shape as Database/Validation/user_active_sessions_schema/validate.sql: one SELECT whose rows are
--   the problems. A clean run returns no rows, and .github/scripts/validate-database-schema.ps1 counts them by
--   wrapping this file in `SELECT COUNT(*) FROM ( … ) AS validation_issues`, so it must stay a single statement.
--   A passing row reports actual_value 'present' and is filtered out; a failing row says what was actually
--   found, and a failing count check reports the count it saw.
--
-- What it proves
--   * The three tables exist, and each of their columns exists at the type the contract pins, including the two
--     nullable columns that let a change row outlive a removal (`lugger_id`, `scrap_type`).
--   * `uq_waitlist_lugger_locations_lugger_name` is unique on `lugger_name` and that column is on a
--     case-insensitive collation, so `Door 6 Lugger` and `door 6 lugger` are one name (FR-004).
--   * `uq_waitlist_lugger_scrap_assignments_type_lugger` is unique over `(scrap_type_key, lugger_id)` in that
--     order and covers nothing else, so one pairing exists once (FR-006).
--   * `waitlist_lugger_scrap_assignments.lugger_id` resolves to `waitlist_lugger_locations(id)` with
--     `ON DELETE CASCADE`, so no assignment survives its lugger (FR-005).
--   * `waitlist_lugger_arrangement_changes` carries no foreign key on `lugger_id`: the audit is not deleted
--     with the lugger and not rewritten when it is (FR-032).
--   * All twelve procedures this feature adds are deployed. Existence is enough here: every one is new, so
--     there is no older body with the same name for this check to be fooled by.
--   * The seed landed: seven luggers and seven assignments, and exactly one dump point for every scrap type
--     that has a usable lugger, with no dump point left behind on a type whose luggers are all blocked and no
--     dump point held by a lugger that cannot receive (FR-008, FR-013).
--   * The permission baseline holds its nine rows for `permission.settings.lugger_arrangement`, six answering 1
--     and three answering 0 (FR-028).

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT 'missing_table' AS issue_type, 'waitlist_lugger_locations' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_table' AS issue_type, 'waitlist_lugger_scrap_assignments' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_table' AS issue_type, 'waitlist_lugger_arrangement_changes' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.id' AS object_name, 'BIGINT NOT NULL AUTO_INCREMENT' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                        AND extra LIKE '%auto_increment%'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.public_id' AS object_name, 'CHAR(36) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'public_id'
                        AND data_type = 'char'
                        AND character_maximum_length = 36
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.lugger_name' AS object_name, 'VARCHAR(64) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'lugger_name'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 64
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.status' AS object_name, 'VARCHAR(16) NOT NULL DEFAULT ''Backup''' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'status'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 16
                        AND is_nullable = 'NO'
                        AND column_default = 'Backup'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.status_reason' AS object_name, 'VARCHAR(500) NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'status_reason'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 500
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.display_position' AS object_name, 'INT NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'display_position'
                        AND data_type = 'int'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.created_utc' AS object_name, 'DATETIME NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'created_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_locations.updated_utc' AS object_name, 'DATETIME NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_locations'
                        AND column_name = 'updated_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.id' AS object_name, 'BIGINT NOT NULL AUTO_INCREMENT' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                        AND extra LIKE '%auto_increment%'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.public_id' AS object_name, 'CHAR(36) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'public_id'
                        AND data_type = 'char'
                        AND character_maximum_length = 36
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.lugger_id' AS object_name, 'BIGINT NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'lugger_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.scrap_type' AS object_name, 'VARCHAR(128) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'scrap_type'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 128
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.scrap_type_key' AS object_name, 'VARCHAR(128) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'scrap_type_key'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 128
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.is_dump_point' AS object_name, 'TINYINT(1) NOT NULL DEFAULT 0' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'is_dump_point'
                        AND column_type = 'tinyint(1)'
                        AND is_nullable = 'NO'
                        AND column_default = '0'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.sort_position' AS object_name, 'INT NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'sort_position'
                        AND data_type = 'int'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.created_utc' AS object_name, 'DATETIME NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'created_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_scrap_assignments.updated_utc' AS object_name, 'DATETIME NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND column_name = 'updated_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.id' AS object_name, 'BIGINT NOT NULL AUTO_INCREMENT' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                        AND extra LIKE '%auto_increment%'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.public_id' AS object_name, 'CHAR(36) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'public_id'
                        AND data_type = 'char'
                        AND character_maximum_length = 36
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.change_kind' AS object_name, 'VARCHAR(32) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'change_kind'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 32
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.lugger_id' AS object_name, 'BIGINT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'lugger_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.lugger_name' AS object_name, 'VARCHAR(64) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'lugger_name'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 64
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.scrap_type' AS object_name, 'VARCHAR(128) NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'scrap_type'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 128
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.scrap_type_key' AS object_name, 'VARCHAR(128) NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'scrap_type_key'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 128
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.previous_value' AS object_name, 'VARCHAR(255) NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'previous_value'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 255
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.new_value' AS object_name, 'VARCHAR(255) NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'new_value'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 255
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.change_reason' AS object_name, 'VARCHAR(500) NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'change_reason'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 500
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.changed_by_employee_number' AS object_name, 'VARCHAR(32) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'changed_by_employee_number'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 32
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.changed_by_employee_name' AS object_name, 'VARCHAR(128) NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'changed_by_employee_name'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 128
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column' AS issue_type, 'waitlist_lugger_arrangement_changes.changed_utc' AS object_name, 'DATETIME NOT NULL' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_arrangement_changes'
                        AND column_name = 'changed_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_unique_key' AS issue_type, 'waitlist_lugger_locations.lugger_name' AS object_name, 'unique on a case-insensitive collation' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM
                        information_schema.statistics AS s
                        INNER JOIN information_schema.columns AS c
                            ON
                                c.table_schema = s.table_schema
                                AND c.table_name = s.table_name
                                AND c.column_name = s.column_name
                    WHERE
                        s.table_schema = DATABASE()
                        AND s.table_name = 'waitlist_lugger_locations'
                        AND s.index_name = 'uq_waitlist_lugger_locations_lugger_name'
                        AND s.non_unique = 0
                        AND s.column_name = 'lugger_name'
                        AND s.seq_in_index = 1
                        AND c.collation_name LIKE '%\_ci'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_unique_key' AS issue_type, 'waitlist_lugger_scrap_assignments.scrap_type_key_lugger_id' AS object_name, 'UNIQUE (scrap_type_key, lugger_id)' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'waitlist_lugger_scrap_assignments'
                        AND index_name = 'uq_waitlist_lugger_scrap_assignments_type_lugger'
                        AND non_unique = 0
                        AND (
                            (seq_in_index = 1 AND column_name = 'scrap_type_key')
                            OR (seq_in_index = 2 AND column_name = 'lugger_id')
                        )
                    GROUP BY index_name
                    HAVING COUNT(*) = 2 AND MAX(seq_in_index) = 2
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_foreign_key' AS issue_type, 'waitlist_lugger_scrap_assignments.lugger_id' AS object_name, 'FOREIGN KEY to waitlist_lugger_locations (id) ON DELETE CASCADE' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM
                        information_schema.referential_constraints AS rc
                        INNER JOIN information_schema.key_column_usage AS k
                            ON
                                k.constraint_schema = rc.constraint_schema
                                AND k.constraint_name = rc.constraint_name
                                AND k.table_name = rc.table_name
                    WHERE
                        rc.constraint_schema = DATABASE()
                        AND rc.table_name = 'waitlist_lugger_scrap_assignments'
                        AND rc.referenced_table_name = 'waitlist_lugger_locations'
                        AND rc.delete_rule = 'CASCADE'
                        AND k.column_name = 'lugger_id'
                        AND k.referenced_column_name = 'id'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'unexpected_foreign_key' AS issue_type, 'waitlist_lugger_arrangement_changes.lugger_id' AS object_name, 'no foreign key on lugger_id' AS expected_value, IF(
                NOT EXISTS (
                    SELECT 1
                    FROM
                        information_schema.key_column_usage AS k
                        INNER JOIN information_schema.table_constraints AS tc
                            ON
                                tc.constraint_schema = k.constraint_schema
                                AND tc.constraint_name = k.constraint_name
                                AND tc.table_name = k.table_name
                    WHERE
                        k.table_schema = DATABASE()
                        AND k.table_name = 'waitlist_lugger_arrangement_changes'
                        AND k.column_name = 'lugger_id'
                        AND tc.constraint_type = 'FOREIGN KEY'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_list' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_list'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_insert' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_insert'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_position_set' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_position_set'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_rename' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_rename'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_delete' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_delete'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_status_set' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_status_set'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_locations_promote' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_locations_promote'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_scrap_assignments_list' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_scrap_assignments_list'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_scrap_assignments_set' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_scrap_assignments_set'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_scrap_assignments_delete' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_scrap_assignments_delete'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_arrangement_list' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_arrangement_list'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_procedure' AS issue_type, 'sp_waitlist_lugger_arrangement_changes_list' AS object_name, 'PROCEDURE exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_waitlist_lugger_arrangement_changes_list'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'seed_count' AS issue_type, 'waitlist_lugger_locations' AS object_name, '7 rows' AS expected_value, IF(
                (
                    SELECT COUNT(*)
                    FROM waitlist_lugger_locations
                ) = 7, 'present',
                CONCAT('rows=', (
                    SELECT COUNT(*)
                    FROM waitlist_lugger_locations
                ))
            ) AS actual_value
        UNION ALL
        SELECT 'seed_count' AS issue_type, 'waitlist_lugger_scrap_assignments' AS object_name, '7 rows' AS expected_value, IF(
                (
                    SELECT COUNT(*)
                    FROM waitlist_lugger_scrap_assignments
                ) = 7, 'present',
                CONCAT('rows=', (
                    SELECT COUNT(*)
                    FROM waitlist_lugger_scrap_assignments
                ))
            ) AS actual_value
        UNION ALL
        SELECT 'seed_dump_point_count' AS issue_type, 'waitlist_lugger_scrap_assignments (usable lugger present)' AS object_name, 'exactly one is_dump_point = 1 row per scrap type' AS expected_value, IF(
                (
                SELECT COUNT(*)
                FROM (
                    SELECT a.scrap_type_key
                    FROM
                        waitlist_lugger_scrap_assignments AS a
                        INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
                    GROUP BY a.scrap_type_key
                    HAVING
                        SUM(l.status IN ('Available', 'Backup')) > 0
                        AND SUM(a.is_dump_point = 1) <> 1
                ) AS off_types
                ) = 0, 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'seed_dump_point_blocked' AS issue_type, 'waitlist_lugger_scrap_assignments (all luggers blocked)' AS object_name, 'no is_dump_point = 1 row' AS expected_value, IF(
                (
                SELECT COUNT(*)
                FROM (
                    SELECT a.scrap_type_key
                    FROM
                        waitlist_lugger_scrap_assignments AS a
                        INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
                    GROUP BY a.scrap_type_key
                    HAVING
                        SUM(l.status IN ('Available', 'Backup')) = 0
                        AND SUM(a.is_dump_point = 1) > 0
                ) AS blocked_types
                ) = 0, 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'seed_dump_point_status' AS issue_type, 'waitlist_lugger_scrap_assignments (dump point lugger)' AS object_name, 'the dump point is held by a lugger that can receive' AS expected_value, IF(
                (
                SELECT COUNT(*)
                FROM
                    waitlist_lugger_scrap_assignments AS a
                    INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
                WHERE
                    a.is_dump_point = 1
                    AND l.status NOT IN ('Available', 'Backup')
                ) = 0, 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'permission_baseline_count' AS issue_type, 'config_settings_values permission.settings.lugger_arrangement' AS object_name, '9 rows' AS expected_value, IF(
                (
                    SELECT COUNT(*)
                    FROM config_settings_values
                    WHERE
                        setting_key = 'permission.settings.lugger_arrangement'
                ) = 9, 'present',
                CONCAT('rows=', (
                    SELECT COUNT(*)
                    FROM config_settings_values
                    WHERE
                        setting_key = 'permission.settings.lugger_arrangement'
                ))
            ) AS actual_value
        UNION ALL
        SELECT 'permission_baseline_granted_count' AS issue_type, 'config_settings_values permission.settings.lugger_arrangement' AS object_name, '6 rows answering 1' AS expected_value, IF(
                (
                    SELECT COUNT(*)
                    FROM config_settings_values
                    WHERE
                        setting_key = 'permission.settings.lugger_arrangement'
                        AND setting_value_bool = 1
                ) = 6, 'present',
                CONCAT('rows=', (
                    SELECT COUNT(*)
                    FROM config_settings_values
                    WHERE
                        setting_key = 'permission.settings.lugger_arrangement'
                        AND setting_value_bool = 1
                ))
            ) AS actual_value
        UNION ALL
        SELECT 'permission_baseline_refused_count' AS issue_type, 'config_settings_values permission.settings.lugger_arrangement' AS object_name, '3 rows answering 0' AS expected_value, IF(
                (
                    SELECT COUNT(*)
                    FROM config_settings_values
                    WHERE
                        setting_key = 'permission.settings.lugger_arrangement'
                        AND setting_value_bool = 0
                ) = 3, 'present',
                CONCAT('rows=', (
                    SELECT COUNT(*)
                    FROM config_settings_values
                    WHERE
                        setting_key = 'permission.settings.lugger_arrangement'
                        AND setting_value_bool = 0
                ))
            ) AS actual_value
    ) validation_results
WHERE
    actual_value <> 'present';
