-- Rollback: mock_requesters
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS mock_requesters;

SET FOREIGN_KEY_CHECKS = 1;
