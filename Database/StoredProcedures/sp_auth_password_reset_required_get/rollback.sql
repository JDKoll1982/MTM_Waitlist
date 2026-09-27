-- Rollback: sp_auth_password_reset_required_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T020)
--
-- Drops the retired read procedure. The artifact used to restore the previous definition rather than drop the
-- procedure, because it was written to reverse the `role_code` column 006-user-management-and-permissions
-- added while the read was still on the sign-in path. That read is retired by this feature, so there is no
-- previous definition left to restore and the reversal is a plain drop, matching every other retired
-- procedure's rollback.
--
-- It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_password_reset_required_get;
