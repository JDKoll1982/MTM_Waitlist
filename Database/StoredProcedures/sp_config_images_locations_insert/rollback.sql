-- Rollback: sp_config_images_locations_insert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094), history row added by 008-part-pictures (task T014)
-- Drops the write procedure and the trigger that recorded its writes. Neither owns data, so the rollback cannot
-- lose a row: the rows the procedure wrote stay in config_images_locations as ordinary data, and the history rows
-- the trigger wrote stay in config_images_locations_history, which the table's own rollback removes.
--
-- The trigger goes first. A trigger left behind after its recorder is gone would keep writing change records for a
-- feature that no longer reads them, and dropping it before the procedure means no insert can land between the
-- two statements and be recorded by a trigger whose reader has just been removed.

USE mtm_waitlist;

DROP TRIGGER IF EXISTS trg_config_images_locations_history_on_insert;

DROP PROCEDURE IF EXISTS sp_config_images_locations_insert;
