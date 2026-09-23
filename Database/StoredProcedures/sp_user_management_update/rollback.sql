-- Rollback: sp_user_management_update
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T012)
--
-- Drops the write. It owns no rows of its own: everything it writes lands in core_users_profiles,
-- auth_roles_assignments and auth_user_management_audit, and each of those has its own rollback.
--
-- Dropping rather than leaving it is the honest reversal. Leaving it would leave a write path whose audit table
-- and whose two new profile columns no longer exist, so it could not run.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_update;
