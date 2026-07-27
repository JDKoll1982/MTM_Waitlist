-- Rollback for table: ops_startup_logs

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS ops_startup_logs;

SET FOREIGN_KEY_CHECKS = 1;