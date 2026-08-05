-- Rollback table: setup_job_history
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS setup_job_history;

SET FOREIGN_KEY_CHECKS = 1;