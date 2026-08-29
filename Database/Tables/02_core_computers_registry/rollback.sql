-- Rollback for table: core_computers_registry

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS core_computers_registry;

SET FOREIGN_KEY_CHECKS = 1;