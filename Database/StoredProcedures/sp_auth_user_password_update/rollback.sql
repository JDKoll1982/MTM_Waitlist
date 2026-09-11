-- Rollback: sp_auth_user_password_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Drops the update added by create.sql. It writes only while it runs, so dropping it cannot lose data.
--
-- **Do not treat this as a password rollback.** It does not restore a previous hash, and it must not: the caller's
-- former inline UPDATE is not restored either, so if this procedure disappears while the application still calls
-- it, a password change fails loudly — the operator sees an error and the existing credential stays valid. That is
-- the correct failure; silently reverting to embedded SQL (or to an older hash) would not be.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_password_update;
