-- Rollback: waitlist_request_subtypes
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS waitlist_request_subtypes;

SET FOREIGN_KEY_CHECKS = 1;
