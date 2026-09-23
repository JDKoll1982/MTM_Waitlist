-- Rollback: sp_auth_temporary_credential_attempt_record
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T009)
--
-- Drops the procedure. It owns no data of its own, so nothing is lost by dropping it: the count it maintains
-- lives on `core_users_profiles.temporary_credential_failed_attempts`, and that column's own rollback is
-- Database/Tables/01_core_users_profiles/rollback.sql.
--
-- Dropping it is the correct reversal rather than a no-op: the procedure did not exist before this feature, so
-- leaving it in place would leave a write path behind that nothing should be calling.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_temporary_credential_attempt_record;
