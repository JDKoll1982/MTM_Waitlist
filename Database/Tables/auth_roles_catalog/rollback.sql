-- Rollback for table: auth_roles_catalog

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS auth_roles_catalog;

SET FOREIGN_KEY_CHECKS = 1;