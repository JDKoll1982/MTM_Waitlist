-- Rollback for table: auth_roles_assignments

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS auth_roles_assignments;

SET FOREIGN_KEY_CHECKS = 1;