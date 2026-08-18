-- Rollback script: config_images_locations
-- Purpose: Drop the config_images_locations table and restore prior state
-- Idempotency: Uses DROP TABLE IF EXISTS for safe re-execution

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS config_images_locations;

SET FOREIGN_KEY_CHECKS = 1;