-- Rollback: sp_auth_user_active_sessions_upsert
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
-- Drops the write procedure. It owns no data: the rows it wrote stay in `user_active_sessions` as ordinary
-- rows, and the table's own rollback is what removes them.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_active_sessions_upsert;
