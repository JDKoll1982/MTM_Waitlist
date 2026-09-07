-- Rollback: mock_locations
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS mock_locations;

SET FOREIGN_KEY_CHECKS = 1;
