-- Rollback: sp_user_management_reset_password
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T013)
--
-- Drops the write. It owns no rows of its own: the credential it sets lives on core_users_profiles and the
-- record of the act lands in auth_user_management_audit, each of which has its own rollback.
--
-- Dropping rather than leaving it is the honest reversal. Leaving it would leave a write path whose audit table
-- and whose attempt-count column no longer exist, so it could not run. It also removes the second of the two
-- writers of the attempt count, which is the state FR-039 describes.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_reset_password;
