-- Create view: vw_config_settings_scope_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

CREATE OR REPLACE VIEW vw_config_settings_scope_catalog AS
SELECT
    setting_key,
    scope_type,
    scope_key,
    workstation_id,
    user_id,
    fn_config_settings_scope_rank (scope_type) AS scope_rank,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc
FROM config_settings_values;

-- View: vw_setup_workstations_active
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP VIEW IF EXISTS vw_setup_workstations_active;

CREATE VIEW vw_setup_workstations_active AS
SELECT
    id,
    public_id,
    building,
    workstation_name,
    is_active,
    sort_rank,
    created_utc,
    updated_utc
FROM setup_workstations_catalog
WHERE
    is_active = 1;