-- Rollback: sp_server_utc_now_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Drops the wrapper added by create.sql. `fn_server_utc_now()` itself is untouched — this artifact never owned it.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_server_utc_now_get;
