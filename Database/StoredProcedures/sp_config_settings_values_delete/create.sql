-- Stored Procedure: sp_config_settings_values_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T096)
--
-- Purpose
-- -------
-- Delete the configuration override stored for one exact (setting_key, scope_key) pair, for
-- `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.DeleteSettingValueAsync`.
--
-- It replaces the caller's inline `DELETE FROM config_settings_values WHERE setting_key = … AND scope_key = …`
-- (FR-015, constitution III). The predicate is identical; only the location of the statement changed.
--
-- Contract
-- --------
--   IN  p_setting_key  VARCHAR(190)   exact setting key (column width: varchar(190))
--   IN  p_scope_key    VARCHAR(255)   exact scope key (column width: varchar(255))
--
--   OUT  no result set. The affected-row count reaches the caller from the client (MySqlConnector's
--        non-query return value); a delete that matches no row is a successful no-op with a count of 0,
--        exactly as the caller's own inline statement behaved.
--
-- Deliberately a hard delete of an override row, not of a primary entity: the override is a disposable
-- wrapper around a value that always has an appsettings.json fallback, which is why removing it is safe and
-- why the caller documents "a missing override must never break configuration resolution". Nothing else in
-- the schema references this row (no foreign key points at config_settings_values).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_delete;

CREATE PROCEDURE sp_config_settings_values_delete(
    IN p_setting_key VARCHAR(190),
    IN p_scope_key VARCHAR(255)
)
DELETE FROM config_settings_values
WHERE setting_key = p_setting_key
  AND scope_key = p_scope_key;
