-- Stored Procedure: sp_config_settings_values_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T091)
--
-- Purpose
-- -------
-- Return the configuration override stored for one exact (setting_key, scope_key) pair, for
-- `MTM_Waitlist.Settings/Services/ConfigSettingsValueService.GetSettingValueAsync`.
--
-- Why this is not sp_config_settings_get_effective (recorded: T091 asked for that one)
-- ---------------------------------------------------------------------------------
-- That procedure answers a different question. It resolves the *effective* value across a precedence
-- chain — `WHERE setting_key = p_setting_key AND (computer match OR all_users OR user match OR admin OR
-- developer)` ordered by `fn_config_settings_scope_rank(scope_type) DESC, updated_utc DESC LIMIT 1` — and
-- it takes `(p_setting_key, p_computer_id, p_user_id)`, not a scope key. It also returns a different column
-- set: no `id`, no `public_id`, no `scope_key`, no `scope_type`.
--
-- The caller needs the *exact* override row for the scope it was asked about, and it parses `id`,
-- `public_id`, `scope_key` and `scope_type` off the result. Routing it to the precedence resolver would
-- have silently changed which row is read (a broader-scope fallback would be returned where the caller
-- expects `null`) and would have dropped four mapped columns. Verified against the live definition before
-- switching, which is exactly what this task asked for.
--
-- Contract
-- --------
--   IN  p_setting_key  VARCHAR(190)   exact setting key (column width: varchar(190))
--   IN  p_scope_key    VARCHAR(255)   exact scope key (column width: varchar(255))
--
--   OUT  zero or one row — the same fifteen columns and the same `LIMIT 1` the inline statement returned.
--
-- Column widths match `config_settings_values` as deployed (setting_key varchar(190), scope_key
-- varchar(255)); a wider parameter would only invite truncation-free mismatches that can never match a row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_get;

CREATE PROCEDURE sp_config_settings_values_get(
    IN p_setting_key VARCHAR(190),
    IN p_scope_key VARCHAR(255)
)
SELECT
    id,
    public_id,
    setting_key,
    scope_type,
    scope_key,
    computer_id,
    user_id,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc
FROM config_settings_values
WHERE setting_key = p_setting_key
  AND scope_key = p_scope_key
LIMIT 1;
