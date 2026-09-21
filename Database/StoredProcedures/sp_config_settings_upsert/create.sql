-- Create procedure: sp_config_settings_upsert
-- Engine: MySQL 5.7
--
-- This is NOT the permission write path, deliberately (006-user-management-and-permissions, task T009).
-- A permission value is written only by `sp_config_permissions_user_set`, which enforces the rank rule, the
-- moved-value check and the one fixed row in one transaction and writes the history row itself. Routing a
-- permission through here would bypass all four.
--
-- The `ELSE 'developer'` branch below therefore stays as it is. It is a known trap, and it is left in place
-- rather than fixed: its job is to give an unrecognised scope a deterministic `scope_key` instead of a NULL,
-- and the `role` scope is never written through this procedure. Seeded role baselines are plain SQL inserts in
-- Database/Seeds/seed_permission_role_baselines, and a person's own rows go through the change-set procedure
-- named above. Adding a `WHEN 'role'` branch here would create a second way to write a role baseline, which is
-- exactly what FR-048 keeps as data and FR-046 keeps in one place.
--
-- The equivalent trap on the read side is worth knowing too: `fn_config_settings_scope_rank` ranks `role` 3 and
-- `user` 6, so a row written with the wrong scope_key is not merely mislabelled, it resolves at the wrong rung.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_upsert;

CREATE PROCEDURE sp_config_settings_upsert(
    IN p_setting_key VARCHAR(190),
    IN p_scope_type VARCHAR(16),
    IN p_computer_id BIGINT,
    IN p_user_id BIGINT,
    IN p_setting_value TEXT,
    IN p_setting_value_int BIGINT,
    IN p_setting_value_bool TINYINT,
    IN p_setting_value_decimal DECIMAL(18, 6),
    IN p_setting_value_datetime_utc DATETIME,
    IN p_value_type VARCHAR(32),
    IN p_updated_by_user_id BIGINT
)
INSERT INTO config_settings_values (
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
)
SELECT
    UUID(),
    TRIM(p_setting_key),
    LOWER(TRIM(p_scope_type)),
    CASE LOWER(TRIM(p_scope_type))
        WHEN 'computer' THEN CONCAT('computer:', p_computer_id)
        WHEN 'user' THEN CONCAT('user:', p_user_id)
        WHEN 'all_users' THEN 'all_users'
        WHEN 'admin' THEN 'admin'
        ELSE 'developer'
    END,
    p_computer_id,
    p_user_id,
    p_setting_value,
    p_setting_value_int,
    p_setting_value_bool,
    p_setting_value_decimal,
    p_setting_value_datetime_utc,
    p_value_type,
    p_updated_by_user_id,
    UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
    setting_value = VALUES(setting_value),
    setting_value_int = VALUES(setting_value_int),
    setting_value_bool = VALUES(setting_value_bool),
    setting_value_decimal = VALUES(setting_value_decimal),
    setting_value_datetime_utc = VALUES(setting_value_datetime_utc),
    value_type = VALUES(value_type),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();