-- Rollback: sp_config_images_locations_computer_sources_all_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
-- Drops the reader. It owns no data: the rows it reads stay in `config_images_locations` as ordinary machine
-- configuration, and the table's own rollback is what removes them.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_all_get;
