-- Rollback script: config_images_locations
-- Purpose: Drop the config_images_locations table and restore prior state
-- Idempotency: Uses DROP TABLE IF EXISTS for safe re-execution
--
-- Feature 008-part-pictures (task T004): the only change this change made to the table was the `scope` column's
-- comment. A comment is not recoverable on its own and is not worth a migration, and the table drop below is
-- already the complete reversal of the table, so this file is deliberately left as the drop it always was.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS config_images_locations;

SET FOREIGN_KEY_CHECKS = 1;