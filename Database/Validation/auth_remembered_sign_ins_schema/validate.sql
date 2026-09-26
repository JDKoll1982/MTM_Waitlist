-- Validate the auth_remembered_sign_ins schema artifacts
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
--
-- What this file is
--   The same shape as Database/Validation/part_pictures_schema/validate.sql: one SELECT whose rows are the
--   problems, filtered to the rows whose actual_value is 'missing'. A clean run returns no rows, and
--   .github/scripts/validate-database-schema.ps1 counts them by wrapping this file in
--   `SELECT COUNT(*) FROM ( … ) AS validation_issues`, so it must stay a single statement.
--
-- What it proves
--   * The table exists with the ten columns the contract pins. `payload_iv VARBINARY(16) NOT NULL` is the
--     assertion that a stored payload can be decrypted: a row without its initialisation vector would never
--     have been written by the upsert and could never be read usefully, so the store refuses to hold one.
--   * Both unique keys exist, and the person-and-machine key is checked over both of its columns because it is
--     what makes a re-remember a replace rather than a second row.
--   * Both foreign keys resolve, so a remembered sign-in can never outlive or outrun the person and machine it
--     names.
--   * The three procedures this feature adds are deployed.

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT
            'missing_table' AS issue_type, 'auth_remembered_sign_ins' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.id', 'BIGINT NOT NULL AUTO_INCREMENT', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                        AND extra LIKE '%auto_increment%'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.public_id', 'CHAR(36) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'public_id'
                        AND data_type = 'char'
                        AND character_maximum_length = 36
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.user_id', 'BIGINT NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'user_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.computer_id', 'BIGINT NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'computer_id'
                        AND data_type = 'bigint'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.payload_ciphertext', 'VARBINARY(512) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'payload_ciphertext'
                        AND data_type = 'varbinary'
                        AND character_maximum_length = 512
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.payload_iv', 'VARBINARY(16) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'payload_iv'
                        AND data_type = 'varbinary'
                        AND character_maximum_length = 16
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.key_fingerprint', 'CHAR(64) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'key_fingerprint'
                        AND data_type = 'char'
                        AND character_maximum_length = 64
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.is_active', 'TINYINT(1) NOT NULL DEFAULT 1', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'is_active'
                        AND data_type = 'tinyint'
                        AND is_nullable = 'NO'
                        AND column_default = '1'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.created_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'created_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_remembered_sign_ins.updated_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND column_name = 'updated_utc'
                        AND data_type = 'datetime'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_key', 'uq_auth_remembered_sign_ins_public_id', 'unique key exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND index_name = 'uq_auth_remembered_sign_ins_public_id'
                        AND non_unique = 0
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_key', 'uq_auth_remembered_sign_ins_user_computer', 'unique key over (user_id, computer_id)', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND index_name = 'uq_auth_remembered_sign_ins_user_computer'
                        AND non_unique = 0
                        AND column_name = 'user_id'
                )
                AND EXISTS (
                    SELECT 1
                    FROM information_schema.statistics
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND index_name = 'uq_auth_remembered_sign_ins_user_computer'
                        AND non_unique = 0
                        AND column_name = 'computer_id'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_foreign_key', 'fk_auth_remembered_sign_ins_core_users_profiles_user_id', 'foreign key to core_users_profiles', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.table_constraints
                    WHERE
                        constraint_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND constraint_name = 'fk_auth_remembered_sign_ins_core_users_profiles_user_id'
                        AND constraint_type = 'FOREIGN KEY'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT
            'missing_foreign_key', 'fk_auth_remembered_sign_ins_core_computers_registry_computer_id',
            'foreign key to core_computers_registry', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.table_constraints
                    WHERE
                        constraint_schema = DATABASE()
                        AND table_name = 'auth_remembered_sign_ins'
                        AND constraint_name = 'fk_auth_remembered_sign_ins_core_computers_registry_computer_id'
                        AND constraint_type = 'FOREIGN KEY'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_remembered_sign_ins_get', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_remembered_sign_ins_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_remembered_sign_ins_upsert', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_remembered_sign_ins_upsert'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_procedure', 'sp_auth_remembered_sign_ins_clear', 'PROCEDURE exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_remembered_sign_ins_clear'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
    ) validation_results
WHERE
    actual_value = 'missing';
