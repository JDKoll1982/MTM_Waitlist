-- Rollback table: setup_active_jobs
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS setup_active_jobs;

SET FOREIGN_KEY_CHECKS = 1;