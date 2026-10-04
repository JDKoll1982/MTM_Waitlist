-- Rollback: sp_auth_user_credential_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T110)
--
-- Drops the read. It is new in this feature, so there is no previous definition to restore and the reversal
-- is a plain drop, matching every other procedure this feature adds.
--
-- It owns no data, so the rollback cannot lose a row. A caller left without this read refuses every sign-in
-- with a stated reason rather than authorising anybody it could not check, which is the failure direction the
-- machine-setup gate and the credential check both already take.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_credential_get;
