-- Validate settings scope schema artifacts
-- Engine: MySQL 5.7

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT
            'missing_table' AS issue_type, 'config_settings_values' AS object_name, 'table exists' AS expected_value, IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_values'
                ), 'present', 'missing'
            ) AS actual_value
        UNION ALL
        SELECT 'missing_table', 'config_settings_history', 'table exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_history'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_table', 'core_buildings_catalog', 'table exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'core_buildings_catalog'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.scope_type', 'VARCHAR(16) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_values'
                        AND column_name = 'scope_type'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.scope_key', 'VARCHAR(255) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_values'
                        AND column_name = 'scope_key'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.workstation_id', 'BIGINT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_values'
                        AND column_name = 'workstation_id'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_values.user_id', 'BIGINT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_values'
                        AND column_name = 'user_id'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_history.scope_type', 'VARCHAR(16) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_history'
                        AND column_name = 'scope_type'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_column', 'config_settings_history.scope_key', 'VARCHAR(255) NOT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_history'
                        AND column_name = 'scope_key'
                        AND is_nullable = 'NO'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_table', 'core_buildings_history', 'table exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'core_buildings_history'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'fn_config_settings_scope_rank', 'function exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'fn_config_settings_scope_rank'
                        AND routine_type = 'FUNCTION'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_settings_get_effective', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_settings_get_effective'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_config_settings_upsert', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_config_settings_upsert'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_routine', 'sp_core_buildings_upsert', 'procedure exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.routines
                    WHERE
                        routine_schema = DATABASE()
                        AND routine_name = 'sp_core_buildings_upsert'
                        AND routine_type = 'PROCEDURE'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'missing_view', 'vw_config_settings_scope_catalog', 'view exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.views
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'vw_config_settings_scope_catalog'
                ), 'present', 'missing'
            )
    ) validation_results
WHERE
    actual_value = 'missing';