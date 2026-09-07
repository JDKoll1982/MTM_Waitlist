-- Rollback: mock_master_tables_registry
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS mock_master_tables_registry;

SET FOREIGN_KEY_CHECKS = 1;
