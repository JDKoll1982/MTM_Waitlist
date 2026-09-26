-- Rollback: sp_ops_startup_logs_insert
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T048)
-- Drops the writer. It owns no data: entries it wrote stay in `ops_startup_logs` with the chain they were
-- written with, and the table's own rollback is what removes the store.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_startup_logs_insert;
