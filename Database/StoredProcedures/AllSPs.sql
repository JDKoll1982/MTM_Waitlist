-- Create procedure: sp_config_settings_get_effective
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_get_effective;

CREATE PROCEDURE sp_config_settings_get_effective(
    IN p_setting_key VARCHAR(190),
    IN p_computer_id BIGINT,
    IN p_user_id BIGINT
)
SELECT
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
    updated_utc,
    fn_config_settings_scope_rank(scope_type) AS scope_rank
FROM config_settings_values
WHERE setting_key = p_setting_key
  AND (
        (scope_type = 'computer' AND computer_id = p_computer_id)
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
-- Purpose: Persist work center setup state.

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
	NULLIF(TRIM(p_selected_dunnage_type_id) COLLATE utf8mb4_unicode_ci, ''),
	NULLIF(TRIM(p_selected_dunnage_part_id) COLLATE utf8mb4_unicode_ci, ''),
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
    NULLIF(TRIM(p_selected_scrap_type) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(p_selected_dunnage_type_id) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(p_selected_dunnage_part_id) COLLATE utf8mb4_unicode_ci, ''),
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
SELECT aj.work_center, aj.work_order, aj.part_number, aj.sequence_number,
       aj.subordinate_parts_json,
       aj.selected_dunnage_parts_json
FROM setup_active_jobs aj
INNER JOIN (
        SELECT work_center, MAX(id) AS max_id
        FROM setup_active_jobs
        WHERE is_active = 1
        GROUP BY work_center
) latest ON latest.max_id = aj.id;

-- Stored Procedure: sp_setup_work_centers_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_delete;

CREATE PROCEDURE sp_setup_work_centers_delete(
    IN p_work_center_id VARCHAR(32)
)
DELETE FROM setup_work_centers_catalog
WHERE id = CAST(TRIM(p_work_center_id) AS UNSIGNED);

-- Stored Procedure: sp_setup_work_centers_get_all
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_get_all;

CREATE PROCEDURE sp_setup_work_centers_get_all()
SELECT
    id,
    building,
    work_center_name,
    is_active,
    updated_utc
FROM vw_setup_work_centers_active
ORDER BY sort_rank ASC, work_center_name ASC;

-- Stored Procedure: sp_setup_work_centers_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_upsert;

CREATE PROCEDURE sp_setup_work_centers_upsert(
    IN p_work_center_id VARCHAR(32),
    IN p_building VARCHAR(64),
    IN p_work_center_name VARCHAR(64),
    IN p_modified_by_user_id BIGINT
)
INSERT INTO setup_work_centers_catalog (
    id,
    public_id,
    building,
    work_center_name,
    is_active,
    sort_rank,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    CAST(NULLIF(TRIM(p_work_center_id) COLLATE utf8mb4_unicode_ci, '') AS UNSIGNED),
    UUID(),
    NULLIF(TRIM(p_building) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(fn_setup_work_center_name_normalized(p_work_center_name) COLLATE utf8mb4_unicode_ci, ''),
    1,
    100,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    building = VALUES(building),
    work_center_name = VALUES(work_center_name),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();

-- Stored Procedure: sp_setup_work_centers_touch
-- Engine: MySQL 5.7
-- Purpose: Refresh the work center catalog "Last Updated" timestamp after setup activity.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_touch;

CREATE PROCEDURE sp_setup_work_centers_touch(
    IN p_work_center VARCHAR(64),
    IN p_updated_by_user_id BIGINT
)
UPDATE setup_work_centers_catalog
SET
    updated_utc = UTC_TIMESTAMP(),
    updated_by_user_id = COALESCE(p_updated_by_user_id, updated_by_user_id)
WHERE work_center_name = TRIM(p_work_center);

-- Stored Procedure: sp_config_hot_workcenters_get_for_computer
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_computer;

CREATE PROCEDURE sp_config_hot_workcenters_get_for_computer(
    IN p_computer_name VARCHAR(128)
)
SELECT
    swc.work_center_name AS work_center_name,
    cwhc.sort_rank
FROM config_computer_hot_work_centers cwhc
INNER JOIN core_computers_registry cwr ON cwr.id = cwhc.computer_id
INNER JOIN setup_work_centers_catalog swc ON swc.id = cwhc.work_center_id
WHERE cwhc.is_active = 1
  AND swc.is_active = 1
  AND (
        cwr.computer_name = TRIM(p_computer_name)
        OR cwr.hostname_normalized = TRIM(p_computer_name)
      )
ORDER BY cwhc.sort_rank ASC, swc.work_center_name ASC;

-- Stored Procedure: sp_config_hot_workcenters_delete_for_computer
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_computer;

CREATE PROCEDURE sp_config_hot_workcenters_delete_for_computer(
    IN p_computer_id BIGINT
)
DELETE FROM config_computer_hot_work_centers
WHERE computer_id = p_computer_id;

-- Stored Procedure: sp_config_hot_workcenters_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_upsert;

CREATE PROCEDURE sp_config_hot_workcenters_upsert(
    IN p_computer_id BIGINT,
    IN p_work_center_id BIGINT,
    IN p_sort_rank INT,
    IN p_modified_by_user_id BIGINT
)
INSERT INTO config_computer_hot_work_centers (
    computer_id,
    work_center_id,
    public_id,
    sort_rank,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    p_computer_id,
    p_work_center_id,
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
    IN p_public_id CHAR(36),
    IN p_building VARCHAR(64),
    IN p_work_center VARCHAR(64),
    IN p_request_type VARCHAR(64),
    IN p_subtype VARCHAR(64),
    IN p_input_value VARCHAR(255),
    IN p_active_setup_job_id VARCHAR(64),
    IN p_work_center_name VARCHAR(64),
    IN p_requester_employee_number VARCHAR(32),
    IN p_requester_employee_name VARCHAR(128),
    IN p_status VARCHAR(32),
    IN p_requested_utc DATETIME,
    IN p_target_time_utc DATETIME,
    IN p_is_overdue TINYINT,
    IN p_assigned_material_handler VARCHAR(128),
    IN p_cancellation_reason VARCHAR(255),
    IN p_note VARCHAR(500)
)
INSERT INTO waitlist_requests_queue (
    public_id,
    building,
    work_center,
    request_type,
    subtype,
    input_value,
    active_setup_job_id,
    work_center_name,
    requester_employee_number,
    requester_employee_name,
    status,
    requested_utc,
    target_time_utc,
    is_overdue,
    assigned_material_handler,
    cancellation_reason,
    note,
    created_utc,
    updated_utc
)
VALUES (
    TRIM(p_public_id),
    TRIM(p_building),
    TRIM(p_work_center),
    TRIM(p_request_type),
    NULLIF(TRIM(COALESCE(p_subtype, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_input_value, '')) COLLATE utf8mb4_unicode_ci, ''),
    TRIM(p_active_setup_job_id),
    TRIM(p_work_center_name),
    TRIM(p_requester_employee_number),
    TRIM(p_requester_employee_name),
    COALESCE(NULLIF(TRIM(COALESCE(p_status, '')) COLLATE utf8mb4_unicode_ci, ''), 'Pending'),
    COALESCE(p_requested_utc, UTC_TIMESTAMP()),
    p_target_time_utc,
    COALESCE(p_is_overdue, 0),
    NULLIF(TRIM(COALESCE(p_assigned_material_handler, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_cancellation_reason, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_note, '')) COLLATE utf8mb4_unicode_ci, ''),
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_waitlist_request_list
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_list;

CREATE PROCEDURE sp_waitlist_request_list(
    IN p_building VARCHAR(64),
    IN p_include_resolved TINYINT
)
SELECT
    id,
    public_id,
    building,
    work_center,
    request_type,
    subtype,
    input_value,
    active_setup_job_id,
    work_center_name,
    requester_employee_number,
    requester_employee_name,
    status,
    requested_utc,
    target_time_utc,
    is_overdue,
    assigned_material_handler,
    cancellation_reason,
    canceled_utc,
    canceled_by_employee_number,
    note,
    accepted_utc,
    completed_utc,
    released_utc,
    created_utc,
    updated_utc
FROM waitlist_requests_queue
WHERE (p_building IS NULL OR TRIM(p_building) = '' OR building = TRIM(p_building))
  AND (p_include_resolved = 1 OR status IN ('Pending', 'Accepted'))
ORDER BY requested_utc ASC;

-- Create procedure: sp_waitlist_request_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_get;

CREATE PROCEDURE sp_waitlist_request_get(
    IN p_public_id CHAR(36)
)
SELECT
    id,
    public_id,
    building,
    work_center,
    request_type,
    subtype,
    input_value,
    active_setup_job_id,
    work_center_name,
    requester_employee_number,
    requester_employee_name,
    status,
    requested_utc,
    target_time_utc,
    is_overdue,
    assigned_material_handler,
    cancellation_reason,
    canceled_utc,
    canceled_by_employee_number,
    note,
    accepted_utc,
    completed_utc,
    released_utc,
    created_utc,
    updated_utc
FROM waitlist_requests_queue
WHERE public_id = p_public_id
LIMIT 1;

-- Create procedure: sp_waitlist_request_status_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_status_update;

CREATE PROCEDURE sp_waitlist_request_status_update(
    IN p_public_id CHAR(36),
    IN p_status VARCHAR(32),
    IN p_assigned_material_handler VARCHAR(128),
    IN p_cancellation_reason VARCHAR(255),
    IN p_canceled_by_employee_number VARCHAR(32),
    IN p_note VARCHAR(500)
)
UPDATE waitlist_requests_queue
SET status = TRIM(p_status),
    assigned_material_handler = NULLIF(TRIM(COALESCE(p_assigned_material_handler, '')) COLLATE utf8mb4_unicode_ci, ''),
    note = COALESCE(NULLIF(TRIM(COALESCE(p_note, '')) COLLATE utf8mb4_unicode_ci, ''), note),
    accepted_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'ACCEPTED'
            THEN COALESCE(accepted_utc, UTC_TIMESTAMP())
        ELSE accepted_utc
    END,
    completed_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'COMPLETED'
            THEN COALESCE(completed_utc, UTC_TIMESTAMP())
        ELSE completed_utc
    END,
    released_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'PENDING'
            THEN COALESCE(released_utc, UTC_TIMESTAMP())
        ELSE released_utc
    END,
    cancellation_reason = CASE
        WHEN UPPER(TRIM(p_status)) = 'CANCELED'
            THEN NULLIF(TRIM(COALESCE(p_cancellation_reason, '')) COLLATE utf8mb4_unicode_ci, '')
        ELSE NULL
    END,
    canceled_utc = CASE
        WHEN UPPER(TRIM(p_status)) = 'CANCELED'
            THEN COALESCE(canceled_utc, UTC_TIMESTAMP())
        ELSE NULL
    END,
    canceled_by_employee_number = CASE
        WHEN UPPER(TRIM(p_status)) = 'CANCELED'
            THEN NULLIF(TRIM(COALESCE(p_canceled_by_employee_number, '')) COLLATE utf8mb4_unicode_ci, '')
        ELSE NULL
    END,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id;

-- Create procedure: sp_waitlist_request_audit_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_audit_insert;

CREATE PROCEDURE sp_waitlist_request_audit_insert(
    IN p_request_public_id CHAR(36),
    IN p_from_status VARCHAR(32),
    IN p_to_status VARCHAR(32),
    IN p_event_type VARCHAR(32),
    IN p_actor_employee_number VARCHAR(32),
    IN p_actor_employee_name VARCHAR(128),
    IN p_details VARCHAR(255)
)
INSERT INTO waitlist_requests_audit (
    public_id,
    request_public_id,
    from_status,
    to_status,
    event_type,
    actor_employee_number,
    actor_employee_name,
    details,
    occurred_utc
)
VALUES (
    UUID(),
    TRIM(p_request_public_id),
    NULLIF(TRIM(COALESCE(p_from_status, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_to_status, '')) COLLATE utf8mb4_unicode_ci, ''),
    TRIM(p_event_type),
    NULLIF(TRIM(COALESCE(p_actor_employee_number, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_actor_employee_name, '')) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(COALESCE(p_details, '')) COLLATE utf8mb4_unicode_ci, ''),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_waitlist_request_types_get
-- Engine: MySQL 5.7
-- Purpose: Return the active real (non-mock) request-type catalog rows for the New-Request wizard.
--          Source of truth for request types (was Assets/Config/waitlist-request-types.json).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_get;

CREATE PROCEDURE sp_waitlist_request_types_get()
SELECT
    id,
    public_id,
    request_type,
    control,
    flow,
    requires_text_input,
    prompt_text,
    min_length,
    max_length,
    default_image_path,
    category,
    item_id,
    center_data_grid_fields_json,
    is_active
FROM waitlist_request_types
WHERE is_active = 1
ORDER BY id ASC;

-- Create procedure: sp_waitlist_request_subtypes_get
-- Engine: MySQL 5.7
-- Purpose: Return the active real (non-mock) request-subtype catalog rows for the New-Request wizard.
--          Source of truth for request subtypes (was Assets/Config/waitlist-request-types.json).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_get;

CREATE PROCEDURE sp_waitlist_request_subtypes_get()
SELECT
    id,
    public_id,
    request_type_id,
    subtype_name,
    control,
    flow,
    requires_text_input,
    prompt_text,
    min_length,
    max_length,
    default_image_path,
    category,
    item_id,
    center_data_grid_fields_json,
    is_active
FROM waitlist_request_subtypes
WHERE is_active = 1
ORDER BY request_type_id ASC, id ASC;

-- Create procedure: sp_waitlist_defect_types_get_all
-- Engine: MySQL 5.7
-- Purpose: List active NCM defect types (for the worker picker and the Module_Settings editor).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_get_all;

CREATE PROCEDURE sp_waitlist_defect_types_get_all()
SELECT
    id,
    public_id,
    defect_name,
    description,
    sort_order,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM waitlist_defect_types
WHERE is_active = 1
ORDER BY sort_order ASC, defect_name ASC;

-- Create procedure: sp_waitlist_defect_types_insert
-- Engine: MySQL 5.7
-- Purpose: Insert a new active NCM defect type into waitlist_defect_types.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_insert;

CREATE PROCEDURE sp_waitlist_defect_types_insert(
    IN p_defect_name VARCHAR(100),
    IN p_description VARCHAR(500),
    IN p_sort_order INT,
    IN p_created_by_user_id BIGINT
)
INSERT INTO waitlist_defect_types (
    public_id, defect_name, description, sort_order, is_active,
    created_by_user_id, updated_by_user_id, created_utc, updated_utc
)
VALUES (
    UUID(),
    NULLIF(TRIM(p_defect_name), ''),
    NULLIF(TRIM(COALESCE(p_description, '')), ''),
    COALESCE(p_sort_order, 0),
    1,
    p_created_by_user_id,
    NULL,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_waitlist_defect_types_update
-- Engine: MySQL 5.7
-- Purpose: Update an existing NCM defect type in waitlist_defect_types by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_update;

CREATE PROCEDURE sp_waitlist_defect_types_update(
    IN p_id BIGINT,
    IN p_defect_name VARCHAR(100),
    IN p_description VARCHAR(500),
    IN p_sort_order INT,
    IN p_updated_by_user_id BIGINT
)
UPDATE waitlist_defect_types
SET defect_name = NULLIF(TRIM(p_defect_name), ''),
    description = NULLIF(TRIM(COALESCE(p_description, '')), ''),
    sort_order = COALESCE(p_sort_order, 0),
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

-- Create procedure: sp_waitlist_defect_types_delete
-- Engine: MySQL 5.7
-- Purpose: Remove an NCM defect type from waitlist_defect_types by id (hard delete).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_delete;

CREATE PROCEDURE sp_waitlist_defect_types_delete(
    IN p_id BIGINT
)
DELETE FROM waitlist_defect_types
WHERE id = p_id;

-- Create procedure: sp_config_settings_values_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T091)
-- Purpose: Read the exact (setting_key, scope_key) override row. Not sp_config_settings_get_effective, which
--          resolves the effective value across the scope precedence chain and takes computer/user ids.

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

-- Create procedure: sp_config_settings_values_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T096)
-- Purpose: Delete the exact (setting_key, scope_key) override row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_values_delete;

CREATE PROCEDURE sp_config_settings_values_delete(
    IN p_setting_key VARCHAR(190),
    IN p_scope_key VARCHAR(255)
)
DELETE FROM config_settings_values
WHERE setting_key = p_setting_key
  AND scope_key = p_scope_key;

-- Create procedure: sp_core_computers_registry_lookup_by_name_mac_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Find the registry row for one exact (computer_name, mac_address_normalized) pair.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_mac_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_name_mac_get(
    IN p_computer_name VARCHAR(128),
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE computer_name = p_computer_name
  AND mac_address_normalized = p_mac_address_normalized
LIMIT 1;

-- Create procedure: sp_core_computers_registry_lookup_by_mac_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Find the most recently updated registry row for a MAC address.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_mac_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_mac_get(
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE mac_address_normalized = p_mac_address_normalized
ORDER BY updated_utc DESC
LIMIT 1;

-- Create procedure: sp_core_computers_registry_lookup_by_name_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Resolve one registry row from a computer name or its normalized hostname, preferring the exact name match.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_name_get(
    IN p_name VARCHAR(255)
)
SELECT
    id,
    computer_name,
    hostname_normalized,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE computer_name = p_name
   OR hostname_normalized = p_name
ORDER BY CASE WHEN computer_name = p_name THEN 0 ELSE 1 END
LIMIT 1;

-- Create procedure: sp_core_computers_registry_registered_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: The registered machines only, in display order — the computer picker's source.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_registered_get;

CREATE PROCEDURE sp_core_computers_registry_registered_get()
SELECT
    computer_name,
    display_name
FROM core_computers_registry
WHERE is_registered = 1
ORDER BY display_name ASC, computer_name ASC;

-- Create procedure: sp_core_computers_registry_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: The whole registry, in display order — the registry editor's source (includes retired machines).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_get_all;

CREATE PROCEDURE sp_core_computers_registry_get_all()
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
ORDER BY display_name ASC, computer_name ASC;

-- Create procedure: sp_core_computers_registry_upsert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Register a computer at logon, or refresh the row it already has.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_upsert;

CREATE PROCEDURE sp_core_computers_registry_upsert(
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255)
)
INSERT INTO core_computers_registry (
    public_id,
    computer_name,
    hostname_normalized,
    mac_address_normalized,
    display_name,
    description,
    is_registered,
    created_utc,
    updated_utc
)
VALUES (
    UUID(),
    p_computer_name,
    p_hostname_normalized,
    p_mac_address_normalized,
    p_display_name,
    p_description,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    computer_name = VALUES(computer_name),
    display_name = VALUES(display_name),
    description = VALUES(description),
    is_registered = 1,
    updated_utc = UTC_TIMESTAMP();

-- Create procedure: sp_core_computers_registry_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Edit one registry row by primary key, including its registration flag.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_update;

CREATE PROCEDURE sp_core_computers_registry_update(
    IN p_id BIGINT,
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255),
    IN p_is_registered TINYINT
)
UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    mac_address_normalized = p_mac_address_normalized,
    display_name = p_display_name,
    description = p_description,
    is_registered = p_is_registered,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

-- Create procedure: sp_core_computers_registry_update_by_mac
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Follow a machine that was renamed — update the newest row for a MAC address.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_update_by_mac;

CREATE PROCEDURE sp_core_computers_registry_update_by_mac(
    IN p_mac_address_normalized VARCHAR(64),
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255)
)
UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    display_name = p_display_name,
    description = p_description,
    updated_utc = UTC_TIMESTAMP()
WHERE mac_address_normalized = p_mac_address_normalized
ORDER BY id DESC
LIMIT 1;

-- Create procedure: sp_core_computers_registry_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Purpose: Remove one registry row by primary key.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_delete;

CREATE PROCEDURE sp_core_computers_registry_delete(
    IN p_id BIGINT
)
DELETE FROM core_computers_registry
WHERE id = p_id;

-- Create procedure: sp_setup_work_centers_catalog_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T090)
-- Purpose: The active work-center catalog with its display rank, ordered by building then rank then name — the
--          order the Settings screen shows. Distinct from sp_setup_work_centers_get_all, which omits sort_rank.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_catalog_get;

CREATE PROCEDURE sp_setup_work_centers_catalog_get()
SELECT
    id,
    work_center_name,
    building,
    sort_rank,
    is_active
FROM vw_setup_work_centers_active
ORDER BY building ASC, sort_rank ASC, work_center_name ASC;

-- Create procedure: sp_server_utc_now_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Purpose: The database server's UTC clock, so session validity is judged against the server, not the client.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_server_utc_now_get;

CREATE PROCEDURE sp_server_utc_now_get()
SELECT fn_server_utc_now() AS server_utc_now;

-- Create procedure: sp_auth_credentials_check
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Purpose: Credential material for one active user, so the caller can verify a supplied password.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_credentials_check;

CREATE PROCEDURE sp_auth_credentials_check(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_name, '') AS role_name,
       u.password_hash,
       u.password_salt,
       u.require_password_change,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;

-- Create procedure: sp_auth_user_row_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Purpose: One active user's identity and role. Same shape as the credentials check minus the credential columns.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_row_get;

CREATE PROCEDURE sp_auth_user_row_get(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;

-- Create procedure: sp_auth_user_password_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Purpose: Store a new password hash/salt for one active user and clear the forced-change flag.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_password_update;

CREATE PROCEDURE sp_auth_user_password_update(
    IN p_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32)
)
UPDATE core_users_profiles
SET password_hash = p_password_hash,
    password_salt = p_password_salt,
    require_password_change = 0,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_user_id
  AND is_active = 1;

-- Create procedure: sp_auth_computer_registered_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Purpose: Whether the presented machine is a registered workstation.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_computer_registered_get;

CREATE PROCEDURE sp_auth_computer_registered_get(
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT COUNT(1) AS registered_count
FROM core_computers_registry
WHERE hostname_normalized = p_hostname_normalized
  AND mac_address_normalized = p_mac_address_normalized
  AND is_registered = 1;

-- Create procedure: sp_auth_session_expiry_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
-- Purpose: The newest live session's expiry for one user, or no row when the user has none.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_session_expiry_get;

CREATE PROCEDURE sp_auth_session_expiry_get(
    IN p_user_id BIGINT
)
SELECT expires_utc
FROM auth_sessions_tokens
WHERE user_id = p_user_id
  AND is_active = 1
  AND revoked_utc IS NULL
ORDER BY expires_utc DESC
LIMIT 1;

-- Create procedure: sp_config_dunnage_types_visibility_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
-- Purpose: The persisted dunnage-type visibility map, one row per stored type.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_get;

CREATE PROCEDURE sp_config_dunnage_types_visibility_get()
SELECT dunnage_type_id, is_visible
FROM config_dunnage_types_visibility;

-- Create procedure: sp_config_dunnage_types_visibility_delete_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
-- Purpose: Clear the whole visibility map before it is rewritten.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_delete_all;

CREATE PROCEDURE sp_config_dunnage_types_visibility_delete_all()
DELETE FROM config_dunnage_types_visibility;

-- Create procedure: sp_config_dunnage_types_visibility_insert_row
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
-- Purpose: Write one dunnage type's visibility row as part of the map rewrite.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_insert_row;

CREATE PROCEDURE sp_config_dunnage_types_visibility_insert_row(
    IN p_dunnage_type_id BIGINT,
    IN p_dunnage_type_name VARCHAR(128),
    IN p_is_visible TINYINT
)
INSERT INTO config_dunnage_types_visibility (
    public_id,
    dunnage_type_id,
    dunnage_type_name,
    is_visible,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    UUID(),
    p_dunnage_type_id,
    p_dunnage_type_name,
    p_is_visible,
    NULL,
    NULL,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_config_images_locations_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: The active image override for one exact (scope, scope_item_id) pair; no row is the caller's
--          "no override" answer. is_active = 1 is part of the identity, not an optional filter.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get;

CREATE PROCEDURE sp_config_images_locations_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1
LIMIT 1;

-- Create procedure: sp_config_images_locations_get_by_scope
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Every active override in one scope, newest first — the order the Settings list shows.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_by_scope;

CREATE PROCEDURE sp_config_images_locations_get_by_scope(
    IN p_scope VARCHAR(16)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1
ORDER BY updated_utc DESC;

-- Create procedure: sp_config_images_locations_count_active_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: How many active overrides exist in total. The column alias `count` is part of the contract — the
--          caller reads the result by name.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_count_active_get;

CREATE PROCEDURE sp_config_images_locations_count_active_get()
SELECT COUNT(*) as count
FROM config_images_locations
WHERE is_active = 1;

-- Create procedure: sp_config_images_locations_count_by_scope_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: How many active overrides one scope holds. Column alias `count`, as above.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_count_by_scope_get;

CREATE PROCEDURE sp_config_images_locations_count_by_scope_get(
    IN p_scope VARCHAR(16)
)
SELECT COUNT(*) as count
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1;

-- Create procedure: sp_config_images_locations_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Every active override, whatever its scope — the input to the orphan detector. No ordering, because
--          the caller checks each row against its own source table and collects the orphans.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_all;

CREATE PROCEDURE sp_config_images_locations_get_all()
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE is_active = 1;

-- Create procedure: sp_config_images_locations_get_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: One active override addressed by its public identifier; no row is "not found". A withdrawn override
--          is not addressable, so the Settings deep-link cannot resurrect one.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_by_public_id;

CREATE PROCEDURE sp_config_images_locations_get_by_public_id(
    IN p_public_id CHAR(36)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE public_id = p_public_id
  AND is_active = 1
LIMIT 1;

-- Create procedure: sp_config_images_locations_recent_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: The most recently updated overrides, capped at p_max_rows. Replaces an interpolated
--          `LIMIT {maxRecordCount}`: MySQL accepts a routine parameter directly in LIMIT, but any expression is
--          rejected (LIMIT GREATEST(p,1) fails with ERROR 1327), so the caller's maxRecordCount < 1 guard is what
--          keeps the cap valid. Deliberately has no is_active filter — this is the audit view and must be able to
--          show a withdrawn override.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_recent_get;

CREATE PROCEDURE sp_config_images_locations_recent_get(
    IN p_max_rows INT
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
ORDER BY updated_utc DESC
LIMIT p_max_rows;

-- Create procedure: sp_setup_work_centers_exists_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Does this work center exist, so the orphan detector can tell a live override from an orphaned one.
--          Deliberately has no is_active filter: filtering to active rows would report every override on a
--          temporarily inactive work center as an orphan and offer a cleanup that should not happen.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_exists_get;

CREATE PROCEDURE sp_setup_work_centers_exists_get(
    IN p_id BIGINT
)
SELECT id
FROM setup_work_centers_catalog
WHERE id = p_id
LIMIT 1;

-- Create procedure: sp_config_images_locations_status_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: The is_active state of one (scope, scope_item_id) pair, whether or not the override is live. The only
--          read of this table with no is_active filter, because the create path must tell "no row" (INSERT) from
--          "inactive row" (reactivate, since uq_config_images_locations_scope_item spans the pair regardless of
--          is_active) from "active row" (DUPLICATE_KEY).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_status_get;

CREATE PROCEDURE sp_config_images_locations_status_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
)
SELECT is_active
FROM config_images_locations
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
LIMIT 1;

-- Create procedure: sp_config_images_locations_insert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Create one override. is_active and both timestamps are generated here; both *_by_user_id columns take
--          the same parameter. Returns 0 or 1 affected rows, and a duplicate-key error still surfaces as 1062.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_insert;

CREATE PROCEDURE sp_config_images_locations_insert(
    IN p_public_id CHAR(36),
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
INSERT INTO config_images_locations (
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    p_public_id,
    p_scope,
    p_scope_item_id,
    p_image_path,
    1,
    p_user_id,
    p_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);

-- Create procedure: sp_config_images_locations_reactivate
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Bring a soft-deleted override back to life with a new image path. No is_active predicate in the WHERE,
--          matching the statement it replaces: the caller only reaches here after the status read reported an
--          existing inactive row, and the unique key means the pair has exactly one row. created_* are untouched.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_reactivate;

CREATE PROCEDURE sp_config_images_locations_reactivate(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    is_active = 1,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id;

-- Create procedure: sp_config_images_locations_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Repoint one live override at a different image. AND is_active = 1 is kept: an update is only
--          meaningful for a live override. created_* are untouched — repointing an image is not a new row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_update;

CREATE PROCEDURE sp_config_images_locations_update(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;

-- Create procedure: sp_config_images_locations_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Withdraw the override for one (scope, scope_item_id) pair — a soft delete, because the unique key
--          spans the pair regardless of is_active and a hard delete would let the next create reuse the pair
--          while losing the audit trail. AND is_active = 1 makes the affected-row count a real "did something
--          change" signal, so withdrawing an already-withdrawn override reports 0 rather than success.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete;

CREATE PROCEDURE sp_config_images_locations_delete(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;

-- Create procedure: sp_config_images_locations_delete_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Withdraw one override addressed by its public identifier. Soft delete, same reasoning as
--          sp_config_images_locations_delete. The affected-row count is the caller's success signal — it used to
--          read a row count after an UPDATE, which is always 0, so every call answered NOT_FOUND.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete_by_public_id;

CREATE PROCEDURE sp_config_images_locations_delete_by_public_id(
    IN p_public_id CHAR(36),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND is_active = 1;

-- Create procedure: sp_config_images_locations_purge_inactive
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Permanently remove every withdrawn override — the only hard delete on the table, and the deliberate
--          escape hatch for the soft-delete design. Unconditional apart from is_active = 0 (no scope parameter).
--          The affected-row count is the caller's return value; it used to read a row count after a DELETE,
--          which is always 0, so every caller was told nothing had been purged.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_purge_inactive;

CREATE PROCEDURE sp_config_images_locations_purge_inactive()
DELETE FROM config_images_locations
WHERE is_active = 0;

-- Create procedure: sp_config_images_locations_deactivate_for_scope
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
-- Purpose: Withdraw every live override in one scope at once — a bulk soft delete, not a purge. AND is_active = 1
--          is kept so an already-withdrawn override is not counted. The affected-row count is the caller's return
--          value; it used to read a row count after an UPDATE, which is always 0, so the UI reported
--          "0 deactivated" however many rows were withdrawn.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_deactivate_for_scope;

CREATE PROCEDURE sp_config_images_locations_deactivate_for_scope(
    IN p_scope VARCHAR(16),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND is_active = 1;
