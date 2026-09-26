-- Rollback: sp_config_images_locations_computer_sources_set
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T051)
-- Drops the writer. It owns no data: the rows it wrote stay in `config_images_locations` as ordinary machine
-- configuration, and the table's own rollback is what removes them.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_set;
