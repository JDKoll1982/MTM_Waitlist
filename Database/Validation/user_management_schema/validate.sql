-- Validate user management schema artifacts (feature 006)
-- Engine: MySQL 5.7
-- Reports one row per missing object and nothing when the schema is whole, so a reader sees issues rather than
-- a pass/fail word. The four columns and the new table are also applied to a store that already exists by the
-- guarded blocks in Database/Bootstrap/update_table_descriptions.sql, which is why they are asserted here as
-- well as in the table artifacts.

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT
            'missing_table' AS issue_type, 'auth_user_management_audit' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.change_group_id', 'VARCHAR(36) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'change_group_id'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.target_user_id', 'BIGINT NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'target_user_id'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.field_name', 'VARCHAR(64) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'field_name'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.previous_value', 'TEXT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'previous_value'
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.changed_value', 'TEXT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'changed_value'
                        AND is_nullable = 'YES'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.actor_role_code', 'VARCHAR(64) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'actor_role_code'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.actor_display_name', 'VARCHAR(256) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'actor_display_name'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.actor_employee_identifier', 'VARCHAR(128) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'actor_employee_identifier'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_user_management_audit.occurred_utc', 'DATETIME NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_user_management_audit'
                        AND column_name = 'occurred_utc'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.first_name', 'VARCHAR(128) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'core_users_profiles'
                        AND column_name = 'first_name'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.last_name', 'VARCHAR(128) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'core_users_profiles'
                        AND column_name = 'last_name'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.temporary_credential_failed_attempts', 'INT NOT NULL DEFAULT 0', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'core_users_profiles'
                        AND column_name = 'temporary_credential_failed_attempts'
                        AND is_nullable = 'NO'
                        AND column_default = '0'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.role_rank', 'INT NOT NULL DEFAULT 0', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'auth_roles_catalog'
                        AND column_name = 'role_rank'
                        AND is_nullable = 'NO'
                        AND column_default = '0'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_user_management_list', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_user_management_list'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_user_management_get', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_user_management_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_user_management_create', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_user_management_create'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_user_management_update', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_user_management_update'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_user_management_reset_password', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_user_management_reset_password'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_permissions_user_get', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_permissions_user_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_auth_temporary_credential_attempt_record', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_temporary_credential_attempt_record'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_auth_credentials_check', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_credentials_check'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_auth_user_row_get', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_user_row_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_auth_password_reset_required_get', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_auth_password_reset_required_get'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
    ) validation_results
WHERE
    actual_value = 'missing';
