-- Rollback: sp_auth_user_password_set
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T116)
--
-- Drops the update. It writes only while it runs, so dropping it cannot lose data and there is no previous
-- definition to restore: the procedure is new in this feature.
--
-- Do not treat this as a credential rollback. It does not restore a previous hash, and it must not. If it
-- disappears while the application still calls it, setting a new password fails loudly and the person's existing
-- credential stays valid, which is the failure direction the sign-in path already takes.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_password_set;
