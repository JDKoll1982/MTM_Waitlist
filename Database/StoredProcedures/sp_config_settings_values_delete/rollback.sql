-- Rollback: sp_config_settings_values_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T096)
--
-- Drops the delete procedure added by create.sql. It owns no data and deletes no row of its own, so the
-- rollback cannot lose anything.
--
-- The caller's former inline DELETE is deliberately NOT restored: dropping this while
-- `ConfigSettingsValueService.DeleteSettingValueAsync` still calls it makes the delete fail loudly instead of
-- quietly reverting to embedded SQL, which is the correct failure for a constitution-III regression.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_delete;
