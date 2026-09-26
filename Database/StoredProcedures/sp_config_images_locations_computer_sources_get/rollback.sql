-- Rollback: sp_config_images_locations_computer_sources_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T051)
-- Drops the reader. It owns no data and changes nothing it reads.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_get;
