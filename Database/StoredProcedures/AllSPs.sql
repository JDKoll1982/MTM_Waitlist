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
FROM dual
ON DUPLICATE KEY UPDATE
	work_order = VALUES(work_order),
	part_number = VALUES(part_number),
	sequence_number = VALUES(sequence_number),
	selected_dunnage_type_id = VALUES(selected_dunnage_type_id),
	selected_dunnage_part_id = VALUES(selected_dunnage_part_id),
	subordinate_parts_json = VALUES(subordinate_parts_json),
	selected_dunnage_parts_json = VALUES(selected_dunnage_parts_json),
	is_active = 1,
	updated_by_user_id = VALUES(updated_by_user_id),
	updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_setup_active_job_dunnage_assignments_get
-- Engine: MySQL 5.7
-- Purpose: Get saved dunnage assignments JSON for an exact part/sequence pair from custom setup data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_active_job_dunnage_assignments_get;

CREATE PROCEDURE sp_setup_active_job_dunnage_assignments_get(
        IN p_work_order VARCHAR(32),
        IN p_part_number VARCHAR(64),
        IN p_sequence_number VARCHAR(32)
)
SELECT selected_dunnage_parts_json
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
    AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_setup_job_history_scrap_type_get
-- Engine: MySQL 5.7
-- Purpose: Get saved scrap type for an exact part/sequence pair from custom setup data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_scrap_type_get;

CREATE PROCEDURE sp_setup_job_history_scrap_type_get(
        IN p_work_order VARCHAR(32),
        IN p_part_number VARCHAR(64),
        IN p_sequence_number VARCHAR(32)
)
SELECT selected_scrap_type
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
    AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_setup_part_sequence_custom_data_upsert
-- Engine: MySQL 5.7
-- Purpose: Upsert custom setup data for an exact part/sequence pair.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_part_sequence_custom_data_upsert;

CREATE PROCEDURE sp_setup_part_sequence_custom_data_upsert(
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32),
    IN p_selected_scrap_type VARCHAR(128),
    IN p_selected_dunnage_type_id VARCHAR(64),
    IN p_selected_dunnage_part_id VARCHAR(64),
    IN p_subordinate_parts_json JSON,
    IN p_selected_dunnage_parts_json JSON,
    IN p_updated_by_user_id BIGINT
)
INSERT INTO setup_part_sequence_custom_data (
    public_id,
    part_number,
    sequence_number,
    selected_scrap_type,
    selected_dunnage_type_id,
    selected_dunnage_part_id,
    subordinate_parts_json,
    selected_dunnage_parts_json,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
SELECT
    UUID(),
    TRIM(p_part_number),
    TRIM(p_sequence_number),
    NULLIF(TRIM(p_selected_scrap_type), ''),
    NULLIF(TRIM(p_selected_dunnage_type_id), ''),
    NULLIF(TRIM(p_selected_dunnage_part_id), ''),
    p_subordinate_parts_json,
    p_selected_dunnage_parts_json,
    p_updated_by_user_id,
    p_updated_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
    selected_scrap_type = VALUES(selected_scrap_type),
    selected_dunnage_type_id = VALUES(selected_dunnage_type_id),
    selected_dunnage_part_id = VALUES(selected_dunnage_part_id),
    subordinate_parts_json = VALUES(subordinate_parts_json),
    selected_dunnage_parts_json = VALUES(selected_dunnage_parts_json),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_setup_job_history_insert_for_pair
-- Engine: MySQL 5.7
-- Purpose: Insert a history snapshot for an exact work order/part/sequence pair from setup_active_jobs.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_insert_for_pair;

CREATE PROCEDURE sp_setup_job_history_insert_for_pair(
        IN p_event_action VARCHAR(32),
        IN p_changed_by_user_id BIGINT,
        IN p_work_order VARCHAR(32),
        IN p_part_number VARCHAR(64),
        IN p_sequence_number VARCHAR(32)
)
INSERT INTO setup_job_history (
        public_id,
        active_job_id,
        event_action,
        work_order,
        part_number,
        sequence_number,
        work_center,
        selected_dunnage_type_id,
        selected_dunnage_part_id,
        subordinate_parts_json,
        selected_dunnage_parts_json,
        changed_by_user_id,
        changed_utc
)
SELECT
        UUID(),
        aj.id,
        TRIM(p_event_action),
        aj.work_order,
        aj.part_number,
        aj.sequence_number,
        aj.work_center,
        aj.selected_dunnage_type_id,
        aj.selected_dunnage_part_id,
        aj.subordinate_parts_json,
        aj.selected_dunnage_parts_json,
        p_changed_by_user_id,
        UTC_TIMESTAMP()
FROM setup_active_jobs aj
WHERE aj.work_order = TRIM(p_work_order)
    AND aj.part_number = TRIM(p_part_number)
    AND aj.sequence_number = TRIM(p_sequence_number)
ORDER BY aj.updated_utc DESC
LIMIT 1;

-- Stored Procedure: sp_setup_active_jobs_latest_by_work_center_get
-- Engine: MySQL 5.7
-- Purpose: Retrieve latest active setup row per work center.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_active_jobs_latest_by_work_center_get;

CREATE PROCEDURE sp_setup_active_jobs_latest_by_work_center_get()
SELECT aj.work_center, aj.work_order, aj.part_number, aj.sequence_number
FROM setup_active_jobs aj
INNER JOIN (
        SELECT work_center, MAX(id) AS max_id
        FROM setup_active_jobs
        WHERE is_active = 1
        GROUP BY work_center
) latest ON latest.max_id = aj.id;

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
    building,
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
    IN p_building VARCHAR(64),
    IN p_workstation_name VARCHAR(64),
    IN p_modified_by_user_id BIGINT
)
INSERT INTO setup_workstations_catalog (
    id,
    public_id,
    building,
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
    NULLIF(TRIM(p_building), ''),
    NULLIF(fn_setup_workstation_name_normalized(p_workstation_name), ''),
    1,
    100,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    building = VALUES(building),
    workstation_name = VALUES(workstation_name),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_config_hot_workcenters_get_for_workstation
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_workstation;

CREATE PROCEDURE sp_config_hot_workcenters_get_for_workstation(
    IN p_workstation_name VARCHAR(128)
)
SELECT
    swc.workstation_name AS work_center_name,
    cwhc.sort_rank
FROM config_workstation_hot_workcenters cwhc
INNER JOIN core_workstations_registry cwr ON cwr.id = cwhc.core_workstation_id
INNER JOIN setup_workstations_catalog swc ON swc.id = cwhc.setup_workstation_id
WHERE cwhc.is_active = 1
  AND swc.is_active = 1
  AND (
        cwr.workstation_name = TRIM(p_workstation_name)
        OR cwr.hostname_normalized = TRIM(p_workstation_name)
      )
ORDER BY cwhc.sort_rank ASC, swc.workstation_name ASC;

-- Stored Procedure: sp_config_hot_workcenters_delete_for_workstation
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_workstation;

CREATE PROCEDURE sp_config_hot_workcenters_delete_for_workstation(
    IN p_core_workstation_id BIGINT
)
DELETE FROM config_workstation_hot_workcenters
WHERE core_workstation_id = p_core_workstation_id;

-- Stored Procedure: sp_config_hot_workcenters_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_upsert;

CREATE PROCEDURE sp_config_hot_workcenters_upsert(
    IN p_core_workstation_id BIGINT,
    IN p_setup_workstation_id BIGINT,
    IN p_sort_rank INT,
    IN p_modified_by_user_id BIGINT
)
INSERT INTO config_workstation_hot_workcenters (
    core_workstation_id,
    setup_workstation_id,
    public_id,
    sort_rank,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    p_core_workstation_id,
    p_setup_workstation_id,
    UUID(),
    p_sort_rank,
    1,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    sort_rank = VALUES(sort_rank),
    is_active = 1,
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();
    
    -- Create procedure: sp_waitlist_request_insert
-- Engine: MySQL 5.7

DROP PROCEDURE IF EXISTS sp_waitlist_request_insert;

CREATE PROCEDURE sp_waitlist_request_insert(
    IN p_building VARCHAR(64),
    IN p_work_center VARCHAR(64),
    IN p_request_type VARCHAR(64),
    IN p_subtype VARCHAR(64),
    IN p_input_value VARCHAR(255),
    IN p_active_setup_job_id VARCHAR(64),
    IN p_workstation_name VARCHAR(64),
    IN p_requester_employee_number VARCHAR(32),
    IN p_requester_employee_name VARCHAR(128),
    IN p_status VARCHAR(32),
    IN p_requested_utc DATETIME,
    IN p_target_time_utc DATETIME,
    IN p_is_overdue TINYINT,
    IN p_assigned_material_handler VARCHAR(128),
    IN p_cancellation_reason VARCHAR(255)
)
INSERT INTO waitlist_requests_queue (
    public_id,
    building,
    work_center,
    request_type,
    subtype,
    input_value,
    active_setup_job_id,
    workstation_name,
    requester_employee_number,
    requester_employee_name,
    status,
    requested_utc,
    target_time_utc,
    is_overdue,
    assigned_material_handler,
    cancellation_reason,
    created_utc,
    updated_utc
)
VALUES (
    UUID(),
    TRIM(p_building),
    TRIM(p_work_center),
    TRIM(p_request_type),
    NULLIF(TRIM(COALESCE(p_subtype, '')), ''),
    NULLIF(TRIM(COALESCE(p_input_value, '')), ''),
    TRIM(p_active_setup_job_id),
    TRIM(p_workstation_name),
    TRIM(p_requester_employee_number),
    TRIM(p_requester_employee_name),
    COALESCE(NULLIF(TRIM(COALESCE(p_status, '')), ''), 'Pending'),
    COALESCE(p_requested_utc, UTC_TIMESTAMP()),
    p_target_time_utc,
    COALESCE(p_is_overdue, 0),
    NULLIF(TRIM(COALESCE(p_assigned_material_handler, '')), ''),
    NULLIF(TRIM(COALESCE(p_cancellation_reason, '')), ''),
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

