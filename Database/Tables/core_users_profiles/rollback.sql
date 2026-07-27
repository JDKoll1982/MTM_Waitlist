-- Rollback for table: core_users_profiles

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS core_users_profiles;

SET FOREIGN_KEY_CHECKS = 1;