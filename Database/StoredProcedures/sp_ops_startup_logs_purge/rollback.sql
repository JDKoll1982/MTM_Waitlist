-- Rollback: sp_ops_startup_logs_purge
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T049)
-- Drops the retention routine. It owns no data and removes nothing by being dropped: the entries it already
-- removed are gone, and that is what a purge means.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_purge;
