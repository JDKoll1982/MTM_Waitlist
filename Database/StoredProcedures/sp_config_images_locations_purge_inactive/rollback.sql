-- Rollback: sp_config_images_locations_purge_inactive
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Drops the purge procedure added by create.sql.
--
-- Note what this rollback cannot undo: this procedure is the one hard DELETE in the set, so any withdrawn
-- overrides it already purged are gone. Dropping the procedure stops the operation, it does not restore rows.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_purge_inactive;
