-- Rollback: sp_user_management_get
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T010)
--
-- Drops the read. It owns no data, so nothing is lost: the row it returns lives in
-- core_users_profiles, auth_roles_assignments and auth_roles_catalog, all of which predate this feature.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_get;
