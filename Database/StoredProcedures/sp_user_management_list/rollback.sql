-- Rollback: sp_user_management_list
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T010)
--
-- Drops the read. It owns no data, so nothing is lost: the roster it returns is composed from
-- core_users_profiles, auth_roles_assignments and auth_roles_catalog, all of which predate this feature.
--
-- Dropping rather than leaving it is the honest reversal: before this feature the application had no roster
-- read at all, so a leftover procedure would be a read path with no caller and no owner.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_list;
