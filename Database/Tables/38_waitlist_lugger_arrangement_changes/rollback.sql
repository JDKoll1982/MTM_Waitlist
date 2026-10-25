-- Rollback: waitlist_lugger_arrangement_changes
-- Engine: MySQL 5.7
-- Reverses: Database/Tables/38_waitlist_lugger_arrangement_changes/create.sql

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS waitlist_lugger_arrangement_changes;

SET FOREIGN_KEY_CHECKS = 1;
