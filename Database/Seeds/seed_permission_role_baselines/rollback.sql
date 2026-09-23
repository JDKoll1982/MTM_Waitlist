-- Rollback: seed_permission_role_baselines
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T036)
--
-- Removes exactly the rows `create.sql` added: the `role`-scoped rows in the `permission.` namespace. Nothing
-- else in this table is touched — the `all_users`-scoped image-storage rows and any `user`-scoped row written
-- since ship day both stay as they are, because this seed did not write them.
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
