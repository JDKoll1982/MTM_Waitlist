-- Rollback: seed_permission_role_baselines
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T036)
--
-- Removes exactly the rows `create.sql` added: the `role`-scoped rows in the `permission.` namespace, and the
-- two `all_users`-scoped plant preference rows 010-startup-rebuild (T053) added. Nothing else in this table is
-- touched — the `all_users`-scoped image-storage rows and any `user`-scoped row written since ship day both stay
-- as they are, because this seed did not write them.
--
-- The delete is narrowed by the same three facts the seed writes with (scope_type `role`, the `permission.`
-- namespace, and a boolean value type), so a `user`-scoped permission row — the kind the permissions page
-- writes, and the kind FR-052 says must be zero on ship day — is not swept up by a rollback of this seed.
--
-- There is nothing to restore: these rows did not exist before this seed ran.

USE mtm_waitlist;

SET NAMES utf8mb4;

DELETE FROM config_settings_values
WHERE scope_type = 'role'
  AND setting_key LIKE 'permission.%'
  AND value_type = 'bool';

-- The two plant-owned rows 010-startup-rebuild (T053) added sit at a different scope, so the DELETE above does
-- not reach them and they are named exactly rather than swept up by a scope-wide predicate. Naming them is what
-- keeps this rollback from removing the `all_users`-scoped image-storage rows, which this seed never wrote.
DELETE FROM config_settings_values
WHERE scope_type = 'all_users'
  AND scope_key = 'all_users'
  AND setting_key IN ('Feature.IgnoredLocations', 'sessions.length_hours');

-- The four keys 010-startup-rebuild (T053) added — `permission.settings.machine_configuration`,
-- `permission.settings.ignored_locations_edit`, `permission.settings.log_panel` and
-- `permission.settings.session_length` — need no statement of their own: every row this seed wrote for them is
-- `role`-scoped, inside the `permission.` namespace and of boolean type, so the DELETE above removes all
-- thirty-six of them along with the rest. Nothing was widened for these keys, which is the case the UPDATE below
-- exists to reverse, so reversing this addition is the DELETE alone.

-- Feature 008-part-pictures (task T006) widened `permission.settings.part_pictures` to two more roles: `setup_lead`
-- and `plant_manager`. Reversing that widening is this UPDATE, which puts exactly those two rows back to the value
-- this seed shipped before the change and touches no other row. The DELETE above is the seed's own full reversal,
-- so on a whole-seed rollback these two rows are already gone and this statement changes nothing; it is here for a
-- store where only the widening is being reversed. `permission.settings.storage_paths` is deliberately NOT named:
-- that key was not widened and its two holders keep it (FR-017).
UPDATE config_settings_values
SET setting_value_bool = 0,
    updated_utc = UTC_TIMESTAMP()
WHERE scope_type = 'role'
  AND value_type = 'bool'
  AND setting_key = 'permission.settings.part_pictures'
  AND scope_key IN ('role:setup_lead', 'role:plant_manager');
