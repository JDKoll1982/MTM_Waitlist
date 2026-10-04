-- Validate the shared MTM_Waitlist schema baseline that survives the startup rebuild
-- Engine: MySQL 5.7
--
-- Reworked by task T021 of specs/010-startup-rebuild: it keeps the checks that cover shared tables and the
-- retained `fn_server_utc_now`, and the per-object checks for the retired startup session table were removed
-- with the surface that used them. The log-store column and index checks stay, because `ops_startup_logs` is
-- repurposed as the rebuilt application's log store rather than retired.

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
        SELECT 'missing_table', 'core_computers_registry', 'table exists', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
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
        SELECT 'missing_column', 'core_computers_registry.computer_name', 'VARCHAR(128) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'computer_name'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.hostname_normalized', 'VARCHAR(255) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'hostname_normalized'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 255
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.mac_address_normalized', 'VARCHAR(64) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'mac_address_normalized'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 64
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.display_name', 'VARCHAR(128) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'display_name'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.description', 'VARCHAR(255) NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'description'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 255
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.is_registered', 'TINYINT(1) NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'is_registered'
                    AND data_type = 'tinyint'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.created_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'created_utc'
                    AND data_type = 'datetime'
                    AND is_nullable = 'NO'
            )
        UNION ALL
        SELECT 'missing_column', 'core_computers_registry.updated_utc', 'DATETIME NOT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'core_computers_registry'
                    AND column_name = 'updated_utc'
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
        -- 010-startup-rebuild (tasks T047 and T050): the four columns the developer panel reads and writes, and
        -- the six indexes it filters on. Each is checked by name and by the columns the panel's query uses: an
        -- index under the right name over the wrong column would pass an existence-only check and still leave
        -- the panel doing a table scan, which is the failure worth catching.
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.module', 'VARCHAR(128) NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'module'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'YES'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.error_type', 'VARCHAR(128) NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'error_type'
                    AND data_type = 'varchar'
                    AND character_maximum_length = 128
                    AND is_nullable = 'YES'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.exception_detail', 'MEDIUMTEXT NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'exception_detail'
                    AND data_type = 'mediumtext'
                    AND is_nullable = 'YES'
            )
        UNION ALL
        SELECT 'missing_column', 'ops_startup_logs.error_fingerprint', 'CHAR(64) NULL', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND column_name = 'error_fingerprint'
                    AND data_type = 'char'
                    AND character_maximum_length = 64
                    AND is_nullable = 'YES'
            )
        UNION ALL
        SELECT 'missing_index', 'idx_ops_startup_logs_level_created_utc', 'index over (level, created_utc)', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.statistics
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND index_name = 'idx_ops_startup_logs_level_created_utc'
                    AND column_name IN ('level', 'created_utc')
                GROUP BY index_name
                HAVING COUNT(DISTINCT column_name) = 2
            )
        UNION ALL
        SELECT 'missing_index', 'idx_ops_startup_logs_host_id_created_utc', 'index over (host_id, created_utc)', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.statistics
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND index_name = 'idx_ops_startup_logs_host_id_created_utc'
                    AND column_name IN ('host_id', 'created_utc')
                GROUP BY index_name
                HAVING COUNT(DISTINCT column_name) = 2
            )
        UNION ALL
        SELECT 'missing_index', 'idx_ops_startup_logs_module_created_utc', 'index over (module, created_utc)', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.statistics
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND index_name = 'idx_ops_startup_logs_module_created_utc'
                    AND column_name IN ('module', 'created_utc')
                GROUP BY index_name
                HAVING COUNT(DISTINCT column_name) = 2
            )
        UNION ALL
        SELECT 'missing_index', 'idx_ops_startup_logs_actor_id_created_utc', 'index over (actor_id, created_utc)', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.statistics
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND index_name = 'idx_ops_startup_logs_actor_id_created_utc'
                    AND column_name IN ('actor_id', 'created_utc')
                GROUP BY index_name
                HAVING COUNT(DISTINCT column_name) = 2
            )
        UNION ALL
        SELECT 'missing_index', 'idx_ops_startup_logs_error_type_created_utc', 'index over (error_type, created_utc)', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.statistics
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND index_name = 'idx_ops_startup_logs_error_type_created_utc'
                    AND column_name IN ('error_type', 'created_utc')
                GROUP BY index_name
                HAVING COUNT(DISTINCT column_name) = 2
            )
        UNION ALL
        SELECT
            'missing_index', 'idx_ops_startup_logs_error_fingerprint_created_utc',
            'index over (error_fingerprint, created_utc)', 'missing'
        WHERE
            NOT EXISTS (
                SELECT 1
                FROM information_schema.statistics
                WHERE
                    table_schema = DATABASE()
                    AND table_name = 'ops_startup_logs'
                    AND index_name = 'idx_ops_startup_logs_error_fingerprint_created_utc'
                    AND column_name IN ('error_fingerprint', 'created_utc')
                GROUP BY index_name
                HAVING COUNT(DISTINCT column_name) = 2
            )
    ) AS validation_issues;
