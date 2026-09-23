-- Rollback: sp_config_images_locations_history_get
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T012)
--
-- Drops the change-record read. It owns no data: the rows it reads live in config_images_locations_history, which
-- the table's own rollback removes. Leaving a read behind after the table is dropped would be a reader pointing
-- at a table that no longer exists.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_history_get;
