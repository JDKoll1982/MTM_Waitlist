-- Validate current startup schema against the expected MTM_Waitlist baseline
-- Engine: MySQL 5.7

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT
            'missing_table' AS issue_type, 'auth_roles_catalog' AS object_name, 'table exists' AS expected_value, 'missing' AS actual_value
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
            )
        UNION ALL
        SELECT 'missing_table', 'core_users_profiles', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
            )
        UNION ALL
        SELECT 'missing_table', 'auth_roles_assignments', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_assignments'
            )
        UNION ALL
        SELECT 'missing_table', 'core_workstations_registry', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
            )
        UNION ALL
        SELECT 'missing_table', 'auth_sessions_tokens', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
            )
        UNION ALL
        SELECT 'missing_table', 'config_settings_values', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_values'
            )
        UNION ALL
        SELECT 'missing_table', 'config_settings_history', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_history'
            )
        UNION ALL
        SELECT 'missing_table', 'ops_startup_logs', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
            )
        UNION ALL
        SELECT 'missing_function', 'fn_server_utc_now', 'FUNCTION exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.routines
                WHERE
                    routine_schema = DATABASE()
                    AND routine_name = 'fn_server_utc_now'
                    AND routine_type = 'FUNCTION'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.id', 'BIGINT NOT NULL AUTO_INCREMENT', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
                    AND column_name = 'id'
                    AND data_type = 'bigint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.public_id', 'CHAR(36) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
                    AND column_name = 'public_id'
                    AND data_type = 'char'
                    AND character_maximum_length = 36
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.role_code', 'VARCHAR(64) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
                    AND column_name = 'role_code'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 64
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.role_name', 'VARCHAR(128) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
                    AND column_name = 'role_name'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.created_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
                    AND column_name = 'created_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_catalog.updated_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_catalog'
                    AND column_name = 'updated_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.public_id', 'CHAR(36) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'public_id'
                    AND data_type = 'char'
                    AND character_maximum_length = 36
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.username_normalized', 'VARCHAR(128) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'username_normalized'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.display_name', 'VARCHAR(256) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'display_name'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 256
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.employee_identifier', 'VARCHAR(128) NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'employee_identifier'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.is_active', 'TINYINT(1) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'is_active'
                    AND data_type = 'tinyint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.created_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'created_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_users_profiles.updated_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_users_profiles'
                    AND column_name = 'updated_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_assignments.user_id', 'BIGINT NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_assignments'
                    AND column_name = 'user_id'
                    AND data_type = 'bigint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_assignments.role_id', 'BIGINT NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_assignments'
                    AND column_name = 'role_id'
                    AND data_type = 'bigint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_assignments.assigned_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_assignments'
                    AND column_name = 'assigned_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_roles_assignments.assigned_by_user_id', 'BIGINT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_roles_assignments'
                    AND column_name = 'assigned_by_user_id'
                    AND data_type = 'bigint'
            )
        UNION ALL
        SELECT 'missing_column', 'core_workstations_registry.workstation_name', 'VARCHAR(128) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
                    AND column_name = 'workstation_name'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_workstations_registry.hostname_normalized', 'VARCHAR(255) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
                    AND column_name = 'hostname_normalized'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 255
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_workstations_registry.mac_address_normalized', 'VARCHAR(64) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
                    AND column_name = 'mac_address_normalized'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 64
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_workstations_registry.is_registered', 'TINYINT(1) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
                    AND column_name = 'is_registered'
                    AND data_type = 'tinyint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_workstations_registry.created_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
                    AND column_name = 'created_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_workstations_registry.updated_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_workstations_registry'
                    AND column_name = 'updated_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.user_id', 'BIGINT NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'user_id'
                    AND data_type = 'bigint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.workstation_id', 'BIGINT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'workstation_id'
                    AND data_type = 'bigint'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.token_hash', 'CHAR(64) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'token_hash'
                    AND data_type = 'char'
                    AND character_maximum_length = 64
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.token_salt', 'VARBINARY(32) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'token_salt'
                    AND data_type = 'varbinary'
                    AND character_maximum_length = 32
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.token_version', 'SMALLINT NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'token_version'
                    AND data_type = 'smallint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.issued_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'issued_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.expires_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'expires_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.revoked_utc', 'DATETIME NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'revoked_utc'
                    AND data_type = 'datetime'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.is_active', 'TINYINT(1) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'is_active'
                    AND data_type = 'tinyint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.source_label', 'VARCHAR(32) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'source_label'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 32
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'auth_sessions_tokens.created_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'auth_sessions_tokens'
                    AND column_name = 'created_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.setting_key', 'VARCHAR(190) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_values'
                    AND column_name = 'setting_key'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 190
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.value_type', 'VARCHAR(32) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_values'
                    AND column_name = 'value_type'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 32
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.updated_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_values'
                    AND column_name = 'updated_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_history.setting_key', 'VARCHAR(190) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_history'
                    AND column_name = 'setting_key'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 190
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_history.value_type', 'VARCHAR(32) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_history'
                    AND column_name = 'value_type'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 32
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_history.changed_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'config_settings_history'
                    AND column_name = 'changed_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.correlation_id', 'CHAR(36) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'correlation_id'
                    AND data_type = 'char'
                    AND character_maximum_length = 36
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.created_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'created_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.level', 'VARCHAR(16) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'level'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 16
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.event_action', 'VARCHAR(128) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'event_action'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.outcome', 'VARCHAR(32) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'outcome'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 32
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.message', 'TEXT NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'message'
                    AND data_type = 'text'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.entry_hash', 'CHAR(64) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'entry_hash'
                    AND data_type = 'char'
                    AND character_maximum_length = 64
                    AND is_nullable = 'NO'
            )
    ) AS validation_issues;