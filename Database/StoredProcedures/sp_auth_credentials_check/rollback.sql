-- Rollback: sp_auth_credentials_check
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T020)
--
-- Drops the retired read procedure. The artifact used to restore the previous definition rather than drop the
-- procedure, because it was written to reverse the two columns 006-user-management-and-permissions added
-- (`role_code` and `temporary_credential_failed_attempts`) while the read was still on the sign-in path. That
-- read is retired by this feature, so there is no previous definition left to restore and the reversal is a
-- plain drop, matching every other retired procedure's rollback.
--
-- It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_credentials_check;
