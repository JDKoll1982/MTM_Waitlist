-- Rollback script: config_images_locations_history
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T005)
-- Idempotency: Uses DROP TABLE IF EXISTS for safe re-execution.
--
-- Dropping this table reverses its creation and nothing else: no other table carries a foreign key to it, and the
-- picture rows it describes are untouched. The history it held is the only thing lost, so a store being rolled
-- back past this point should be treated as having no picture change record rather than an incomplete one.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS config_images_locations_history;

SET FOREIGN_KEY_CHECKS = 1;
