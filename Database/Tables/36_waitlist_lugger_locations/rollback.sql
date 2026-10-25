-- Rollback: waitlist_lugger_locations
-- Engine: MySQL 5.7
-- Reverses: Database/Tables/36_waitlist_lugger_locations/create.sql
-- The assignments and the change rows are rolled back by their own scripts; the change table has no foreign
-- key back to this one, so nothing here needs to survive for its sake.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS waitlist_lugger_locations;

SET FOREIGN_KEY_CHECKS = 1;
