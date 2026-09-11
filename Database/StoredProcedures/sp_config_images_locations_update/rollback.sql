-- Rollback: sp_config_images_locations_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Drops the write procedure added by create.sql. It owns no data, so the rollback cannot lose a row; the image
-- paths it was used to change stay in config_images_locations as ordinary data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_update;
