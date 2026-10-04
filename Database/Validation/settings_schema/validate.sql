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
        SELECT 'missing_column', 'config_settings_values.computer_id', 'BIGINT NULL', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.columns
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'config_settings_values'
                        AND column_name = 'computer_id'
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
        SELECT 'missing_view', 'vw_config_settings_scope_catalog', 'view exists', IF(
                EXISTS (
                    SELECT 1
                    FROM information_schema.views
                    WHERE
                        table_schema = DATABASE()
                        AND table_name = 'vw_config_settings_scope_catalog'
                ), 'present', 'missing'
            )
        UNION ALL
        SELECT 'scope_rank_regression', 'fn_config_settings_scope_rank(computer)', '1', IF(
                fn_config_settings_scope_rank('computer') = 1, 'present', 'missing'
            )
        UNION ALL
        SELECT 'scope_rank_regression', 'fn_config_settings_scope_rank(all_users)', '2', IF(
                fn_config_settings_scope_rank('all_users') = 2, 'present', 'missing'
            )
        UNION ALL
        SELECT 'scope_rank_regression', 'fn_config_settings_scope_rank(role)', '3', IF(
                fn_config_settings_scope_rank('role') = 3, 'present', 'missing'
            )
        UNION ALL
        SELECT 'scope_rank_regression', 'fn_config_settings_scope_rank(admin)', '4', IF(
                fn_config_settings_scope_rank('admin') = 4, 'present', 'missing'
            )
        UNION ALL
        SELECT 'scope_rank_regression', 'fn_config_settings_scope_rank(developer)', '5', IF(
                fn_config_settings_scope_rank('developer') = 5, 'present', 'missing'
            )
        UNION ALL
        SELECT 'scope_rank_regression', 'fn_config_settings_scope_rank(user)', '6 above every wider scope', IF(
                fn_config_settings_scope_rank('user') = 6
                AND fn_config_settings_scope_rank('user') > fn_config_settings_scope_rank('role')
                AND fn_config_settings_scope_rank('user') > fn_config_settings_scope_rank('admin')
                AND fn_config_settings_scope_rank('user') > fn_config_settings_scope_rank('developer'), 'present', 'missing'
            )
        UNION ALL
        -- 010-startup-rebuild (task T053): the four keys the new privileged surfaces read must each carry a
        -- `role`-scoped baseline row for every role the catalogue holds, not merely for the two the contract names.
        -- The expected role set is therefore derived from `auth_roles_catalog` itself rather than written out
        -- here, so a role added later is covered without this file being edited and the check cannot drift from
        -- the catalogue.
        --
        -- One role is excluded by name: `admin` is the retired role, which `seed_role_admin_to_it_department`
        -- replaces with `it_department` and for which `seed_permission_role_baselines` therefore writes no rows
        -- by design. Expecting a baseline for it reports four findings that can never be satisfied - one per key
        -- - so the check no longer expects the retired role. The exclusion is named rather than expressed as a
        -- role list on purpose: the roles stay derived, and the single retirement is the only thing stated here.
        SELECT
            'missing_role_baseline' AS issue_type,
            CONCAT(k.setting_key, ' for role:', r.role_code) AS object_name,
            'role-scoped boolean baseline row' AS expected_value,
            IF(v.id IS NULL, 'missing', 'present') AS actual_value
        FROM (
            SELECT 'permission.settings.machine_configuration' AS setting_key
            UNION ALL SELECT 'permission.settings.ignored_locations_edit'
            UNION ALL SELECT 'permission.settings.log_panel'
            UNION ALL SELECT 'permission.settings.session_length'
        ) AS k
        CROSS JOIN auth_roles_catalog AS r
        LEFT JOIN config_settings_values AS v
            ON v.setting_key = k.setting_key
            AND v.scope_type = 'role'
            AND v.scope_key = CONCAT('role:', r.role_code)
            AND v.value_type = 'bool'
        WHERE r.role_code <> 'admin'
        UNION ALL
        -- 010-startup-rebuild (task T053): the two plant-owned preference values must each carry an `all_users`
        -- row, which is the scope `fn_config_settings_scope_rank` ranks 2 and `sp_config_settings_upsert` writes
        -- with `scope_key = 'all_users'`. The value type is pinned as well, because a row written into the wrong
        -- column is one the effective-settings read cannot answer from even though the row exists.
        SELECT
            'missing_plant_preference' AS issue_type,
            p.setting_key AS object_name,
            CONCAT(p.expected_value_type, '-valued row at scope all_users') AS expected_value,
            IF(v.id IS NULL, 'missing', 'present') AS actual_value
        FROM (
            SELECT 'Feature.IgnoredLocations' AS setting_key, 'text' AS expected_value_type
            UNION ALL SELECT 'auth.session_length_minutes', 'int'
        ) AS p
        LEFT JOIN config_settings_values AS v
            ON v.setting_key = p.setting_key
            AND v.scope_type = 'all_users'
            AND v.scope_key = 'all_users'
            AND v.value_type = p.expected_value_type
    ) validation_results
WHERE
    actual_value = 'missing';