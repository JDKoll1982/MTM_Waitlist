-- Rollback: sp_config_settings_values_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T091)
--
-- Drops the read procedure added by create.sql. It owns no data, so the rollback cannot lose a row.
--
-- The caller's former inline SELECT is deliberately NOT restored: if this procedure is dropped while
-- `ConfigSettingsValueService` still calls it, that read fails loudly (the service catches, logs and returns
-- null, which is its documented "no override" behaviour) rather than quietly reverting to embedded SQL.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_get;
