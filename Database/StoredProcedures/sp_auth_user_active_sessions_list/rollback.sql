-- Rollback: sp_auth_user_active_sessions_list
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
-- Drops the support read. It owns no data and changes nothing it reads.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_active_sessions_list;
