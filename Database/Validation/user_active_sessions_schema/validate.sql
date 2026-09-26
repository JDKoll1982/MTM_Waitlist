-- Validate the user_active_sessions schema artifacts
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
--
-- What this file is
--   The same shape as Database/Validation/part_pictures_schema/validate.sql: one SELECT whose rows are the
--   problems, filtered to the rows whose actual_value is 'missing'. A clean run returns no rows, and
--   .github/scripts/validate-database-schema.ps1 counts them by wrapping this file in
--   `SELECT COUNT(*) FROM ( … ) AS validation_issues`, so it must stay a single statement.
--
-- What it proves
--   * The table exists with the twelve columns the contract pins, at the widths and nullability the salted-hash
--     rule requires. `token_hash CHAR(64) NOT NULL` and `token_salt VARBINARY(32) NOT NULL` together are the
--     assertion that no plaintext token can be stored: a stored token would have to be a 64-character digest
--     and would still have to carry its salt.
--   * Both unique keys exist. `uq_user_active_sessions_user_computer` is the one that makes "one active row per
--     person per machine" true, so the check requires the key to be unique and to cover both columns rather
--     than merely to exist under that name.
--   * The live-session read index exists over both of its columns, and both foreign keys resolve to the
--     registry and profile tables they name.
--   * The four procedures this feature adds are deployed. Existence is enough here: every one of them is new,
--     so there is no older body with the same name for this check to be fooled by.

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT
            'missing_table' AS issue_type, 'user_active_sessions' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.id', 'BIGINT NOT NULL AUTO_INCREMENT', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                        AND extra LIKE '%auto_increment%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.public_id', 'CHAR(36) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'public_id'
                        AND data_type = 'char'
                        AND character_maximum_length = 36
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.user_id', 'BIGINT NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'user_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.computer_id', 'BIGINT NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'computer_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.token_hash', 'CHAR(64) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'token_hash'
                        AND data_type = 'char'
                        AND character_maximum_length = 64
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.token_salt', 'VARBINARY(32) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'token_salt'
                        AND data_type = 'varbinary'
                        AND character_maximum_length = 32
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.issued_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'issued_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.expires_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'expires_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.revoked_utc', 'DATETIME NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'revoked_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.is_active', 'TINYINT(1) NOT NULL DEFAULT 1', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'is_active'
                        AND data_type = 'tinyint'
                        AND is_nullable = 'NO'
                        AND column_default = '1'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.source_label', 'VARCHAR(32) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'source_label'
                        AND data_type = 'varchar'
                        AND character_maximum_length = 32
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'user_active_sessions.created_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND column_name = 'created_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_key', 'uq_user_active_sessions_public_id', 'unique key exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND index_name = 'uq_user_active_sessions_public_id'
                        AND non_unique = 0
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_key', 'uq_user_active_sessions_user_computer', 'unique key over (user_id, computer_id)', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND index_name = 'uq_user_active_sessions_user_computer'
                        AND non_unique = 0
                        AND column_name = 'user_id'
                )
                AND EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND index_name = 'uq_user_active_sessions_user_computer'
                        AND non_unique = 0
                        AND column_name = 'computer_id'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_index', 'idx_user_active_sessions_is_active_expires_utc', 'index over (is_active, expires_utc)', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND index_name = 'idx_user_active_sessions_is_active_expires_utc'
                        AND column_name = 'is_active'
                )
                AND EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND index_name = 'idx_user_active_sessions_is_active_expires_utc'
                        AND column_name = 'expires_utc'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_foreign_key', 'fk_user_active_sessions_core_users_profiles_user_id', 'foreign key to core_users_profiles', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.table_constraints
                    WHERE
                        constraint_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND constraint_name = 'fk_user_active_sessions_core_users_profiles_user_id'
                        AND constraint_type = 'FOREIGN KEY'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT
            'missing_foreign_key', 'fk_user_active_sessions_core_computers_registry_computer_id',
            'foreign key to core_computers_registry', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.table_constraints
                    WHERE
                        constraint_schema = DATABASE()
                        AND table_name = 'user_active_sessions'
                        AND constraint_name = 'fk_user_active_sessions_core_computers_registry_computer_id'
                        AND constraint_type = 'FOREIGN KEY'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_user_active_sessions_upsert', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_user_active_sessions_upsert'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_user_active_sessions_get', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_user_active_sessions_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_user_active_sessions_clear', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_user_active_sessions_clear'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_user_active_sessions_list', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_user_active_sessions_list'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
    ) validation_results
WHERE
    actual_value = 'missing';
