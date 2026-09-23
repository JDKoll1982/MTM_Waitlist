-- Rollback: sp_config_images_locations_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094), history row added by 008-part-pictures (task T015)
-- Drops the write procedure and the trigger that recorded its writes. Neither owns data, so the rollback cannot
-- lose a row: the image paths it was used to change stay in config_images_locations as ordinary data, and the
-- history rows the trigger wrote stay in config_images_locations_history, which the table's own rollback removes.
--
-- The trigger goes first, so no update can land between the two statements and be recorded by a trigger whose
-- reader has just been removed.

USE mtm_waitlist;

DROP TRIGGER IF EXISTS trg_config_images_locations_history_on_update;

DROP PROCEDURE IF EXISTS sp_config_images_locations_update;
