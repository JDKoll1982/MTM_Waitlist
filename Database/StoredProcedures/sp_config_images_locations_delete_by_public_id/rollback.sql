-- Rollback: sp_config_images_locations_delete_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Drops the write procedure added by create.sql. It owns no data, so the rollback cannot lose a row; the rows it
-- was used to withdraw stay in config_images_locations, still flagged is_active = 0.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete_by_public_id;
