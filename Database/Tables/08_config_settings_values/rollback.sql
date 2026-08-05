-- Rollback for table: config_settings_values

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS config_settings_values;

SET FOREIGN_KEY_CHECKS = 1;