-- Rollback: sp_ops_startup_logs_filter
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T049)
-- Drops the panel's reader. It owns no data and changes nothing it reads.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_filter;
