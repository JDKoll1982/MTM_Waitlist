-- Rollback: mock_work_centers
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS mock_work_centers;

SET FOREIGN_KEY_CHECKS = 1;
