-- Rollback: waitlist_lugger_scrap_assignments
-- Engine: MySQL 5.7
-- Reverses: Database/Tables/37_waitlist_lugger_scrap_assignments/create.sql

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS waitlist_lugger_scrap_assignments;

SET FOREIGN_KEY_CHECKS = 1;
