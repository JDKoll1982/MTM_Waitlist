-- Create procedure: sp_config_settings_get_effective
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_get_effective;

CREATE PROCEDURE sp_config_settings_get_effective(
    IN p_setting_key VARCHAR(190),
    IN p_workstation_id BIGINT,
    IN p_user_id BIGINT
)
SELECT
    setting_key,
    scope_type,
    scope_key,
    workstation_id,
    user_id,
    setting_value,
    setting_value_int,
    setting_value_bool,
    setting_value_decimal,
    setting_value_datetime_utc,
    value_type,
    updated_by_user_id,
    updated_utc,
    fn_config_settings_scope_rank(scope_type) AS scope_rank
FROM config_settings_values
WHERE setting_key = p_setting_key
  AND (
        (scope_type = 'workstation' AND workstation_id = p_workstation_id)
        OR scope_type = 'all_users'
        OR (scope_type = 'user' AND user_id = p_user_id)
        OR scope_type IN ('admin', 'developer')
      )
ORDER BY fn_config_settings_scope_rank(scope_type) DESC, updated_utc DESC
LIMIT 1;

-- Create procedure: sp_config_settings_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_upsert;

CREATE PROCEDURE sp_config_settings_upsert(
    IN p_setting_key VARCHAR(190),
    IN p_scope_type VARCHAR(16),
    IN p_workstation_id BIGINT,
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
    workstation_id,
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
        WHEN 'workstation' THEN CONCAT('workstation:', p_workstation_id)
        WHEN 'user' THEN CONCAT('user:', p_user_id)
        WHEN 'all_users' THEN 'all_users'
        WHEN 'admin' THEN 'admin'
        ELSE 'developer'
    END,
    p_workstation_id,
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
    
    -- Create procedure: sp_core_buildings_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_buildings_upsert;

CREATE PROCEDURE sp_core_buildings_upsert(
    IN p_building_code VARCHAR(64),
    IN p_building_name VARCHAR(128),
    IN p_is_active TINYINT,
    IN p_updated_by_user_id BIGINT
)
INSERT INTO core_buildings_catalog (
    public_id,
    building_code,
    building_name,
    is_active,
    created_utc,
    updated_utc,
    updated_by_user_id
)
SELECT
    UUID(),
    TRIM(p_building_code),
    TRIM(p_building_name),
    p_is_active,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP(),
    p_updated_by_user_id
FROM dual
ON DUPLICATE KEY UPDATE
    building_name = VALUES(building_name),
    is_active = VALUES(is_active),
    updated_utc = UTC_TIMESTAMP(),
    updated_by_user_id = VALUES(updated_by_user_id);
    
    -- Stored Procedure: sp_setup_save_setup
-- Engine: MySQL 5.7
-- Purpose: Persist workstation setup state.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_save_setup;

CREATE PROCEDURE sp_setup_save_setup(
	IN p_work_order VARCHAR(32),
	IN p_part_number VARCHAR(64),
	IN p_sequence_number VARCHAR(32),
	IN p_work_center VARCHAR(64),
	IN p_selected_dunnage_type_id VARCHAR(64),
	IN p_selected_dunnage_part_id VARCHAR(64),
	IN p_subordinate_parts_json JSON,
	IN p_selected_dunnage_parts_json JSON,
	IN p_saved_by_user_id BIGINT
)
INSERT INTO setup_active_jobs (
	public_id,
	work_order,
	part_number,
	sequence_number,
	work_center,
	selected_dunnage_type_id,
	selected_dunnage_part_id,
	subordinate_parts_json,
	selected_dunnage_parts_json,
	is_active,
	created_by_user_id,
	updated_by_user_id,
	created_utc,
	updated_utc
)
SELECT
	UUID(),
	TRIM(p_work_order),
	TRIM(p_part_number),
	TRIM(p_sequence_number),
	TRIM(p_work_center),
	NULLIF(TRIM(p_selected_dunnage_type_id), ''),
	NULLIF(TRIM(p_selected_dunnage_part_id), ''),
	p_subordinate_parts_json,
	p_selected_dunnage_parts_json,
	1,
	p_saved_by_user_id,
	p_saved_by_user_id,
	UTC_TIMESTAMP(),
	UTC_TIMESTAMP()
FROM dual;

-- Stored Procedure: sp_setup_workstations_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_delete;

CREATE PROCEDURE sp_setup_workstations_delete(
    IN p_workstation_id VARCHAR(32)
)
DELETE FROM setup_workstations_catalog
WHERE id = CAST(TRIM(p_workstation_id) AS UNSIGNED);

-- Stored Procedure: sp_setup_workstations_get_all
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_get_all;

CREATE PROCEDURE sp_setup_workstations_get_all()
SELECT
    id,
    workstation_name,
    is_active
FROM vw_setup_workstations_active
ORDER BY sort_rank ASC, workstation_name ASC;

-- Stored Procedure: sp_setup_workstations_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_upsert;

CREATE PROCEDURE sp_setup_workstations_upsert(
    IN p_workstation_id VARCHAR(32),
    IN p_workstation_name VARCHAR(64),
    IN p_modified_by_user_id BIGINT
)
INSERT INTO setup_workstations_catalog (
    id,
    public_id,
    workstation_name,
    is_active,
    sort_rank,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    CAST(NULLIF(TRIM(p_workstation_id), '') AS UNSIGNED),
    UUID(),
    NULLIF(fn_setup_workstation_name_normalized(p_workstation_name), ''),
    1,
    100,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    workstation_name = VALUES(workstation_name),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();
    
    