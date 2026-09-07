-- Rollback for table: waitlist_requests_audit

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS waitlist_requests_audit;

SET FOREIGN_KEY_CHECKS = 1;
