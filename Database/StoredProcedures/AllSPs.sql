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


-- Create procedure: sp_mock_inventory_locations_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_delete;

CREATE PROCEDURE sp_mock_inventory_locations_delete(IN p_id BIGINT)
DELETE FROM mock_inventory_locations WHERE id = p_id;


-- Create procedure: sp_mock_inventory_locations_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_get;

CREATE PROCEDURE sp_mock_inventory_locations_get()
SELECT id, public_id, part_number, location_code, on_hand_quantity, created_utc, updated_utc
FROM mock_inventory_locations
ORDER BY id ASC;


-- Create procedure: sp_mock_inventory_locations_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_insert;

CREATE PROCEDURE sp_mock_inventory_locations_insert(
    IN p_part_number VARCHAR(50),
    IN p_location_code VARCHAR(50),
    IN p_on_hand_quantity DECIMAL(18,2)
)
INSERT INTO mock_inventory_locations (part_number, location_code, on_hand_quantity, public_id, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_part_number), ''), NULLIF(TRIM(p_location_code), ''), p_on_hand_quantity, UUID(), UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_inventory_locations_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_update;

CREATE PROCEDURE sp_mock_inventory_locations_update(
    IN p_id BIGINT,
    IN p_part_number VARCHAR(50),
    IN p_location_code VARCHAR(50),
    IN p_on_hand_quantity DECIMAL(18,2)
)
UPDATE mock_inventory_locations
SET part_number = NULLIF(TRIM(p_part_number), ''), location_code = NULLIF(TRIM(p_location_code), ''), on_hand_quantity = p_on_hand_quantity, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_locations_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_delete;

CREATE PROCEDURE sp_mock_locations_delete(IN p_id BIGINT)
DELETE FROM mock_locations WHERE id = p_id;


-- Create procedure: sp_mock_locations_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_get;

CREATE PROCEDURE sp_mock_locations_get()
SELECT id, public_id, location_code, location_description, is_ignored, is_active, created_utc, updated_utc
FROM mock_locations
ORDER BY id ASC;


-- Create procedure: sp_mock_locations_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_insert;

CREATE PROCEDURE sp_mock_locations_insert(
    IN p_location_code VARCHAR(50),
    IN p_location_description VARCHAR(255),
    IN p_is_ignored TINYINT
)
INSERT INTO mock_locations (location_code, location_description, is_ignored, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_location_code), ''), NULLIF(TRIM(p_location_description), ''), p_is_ignored, UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_locations_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_update;

CREATE PROCEDURE sp_mock_locations_update(
    IN p_id BIGINT,
    IN p_location_code VARCHAR(50),
    IN p_location_description VARCHAR(255),
    IN p_is_ignored TINYINT
)
UPDATE mock_locations
SET location_code = NULLIF(TRIM(p_location_code), ''), location_description = NULLIF(TRIM(p_location_description), ''), is_ignored = p_is_ignored, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_master_tables_registry_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_delete;

CREATE PROCEDURE sp_mock_master_tables_registry_delete(IN p_id BIGINT)
DELETE FROM mock_master_tables_registry WHERE id = p_id;


-- Create procedure: sp_mock_master_tables_registry_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_get;

CREATE PROCEDURE sp_mock_master_tables_registry_get()
SELECT id, public_id, table_name, ui_display_name, description_text, sort_rank, is_active, created_utc, updated_utc
FROM mock_master_tables_registry
ORDER BY id ASC;


-- Create procedure: sp_mock_master_tables_registry_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_insert;

CREATE PROCEDURE sp_mock_master_tables_registry_insert(
    IN p_table_name VARCHAR(128),
    IN p_ui_display_name VARCHAR(128),
    IN p_description_text VARCHAR(500),
    IN p_sort_rank INT
)
INSERT INTO mock_master_tables_registry (table_name, ui_display_name, description_text, sort_rank, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_table_name), ''), NULLIF(TRIM(p_ui_display_name), ''), NULLIF(TRIM(p_description_text), ''), p_sort_rank, UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_master_tables_registry_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_update;

CREATE PROCEDURE sp_mock_master_tables_registry_update(
    IN p_id BIGINT,
    IN p_table_name VARCHAR(128),
    IN p_ui_display_name VARCHAR(128),
    IN p_description_text VARCHAR(500),
    IN p_sort_rank INT
)
UPDATE mock_master_tables_registry
SET table_name = NULLIF(TRIM(p_table_name), ''), ui_display_name = NULLIF(TRIM(p_ui_display_name), ''), description_text = NULLIF(TRIM(p_description_text), ''), sort_rank = p_sort_rank, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_parts_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_delete;

CREATE PROCEDURE sp_mock_parts_delete(IN p_id BIGINT)
DELETE FROM mock_parts WHERE id = p_id;


-- Create procedure: sp_mock_parts_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_get;

CREATE PROCEDURE sp_mock_parts_get()
SELECT id, public_id, part_number, part_category, part_description, unit_of_measure, is_active, created_utc, updated_utc
FROM mock_parts
ORDER BY id ASC;


-- Create procedure: sp_mock_parts_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_insert;

CREATE PROCEDURE sp_mock_parts_insert(
    IN p_part_number VARCHAR(50),
    IN p_part_category VARCHAR(32),
    IN p_part_description VARCHAR(500),
    IN p_unit_of_measure VARCHAR(20)
)
INSERT INTO mock_parts (part_number, part_category, part_description, unit_of_measure, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_part_number), ''), NULLIF(TRIM(p_part_category), ''), NULLIF(TRIM(p_part_description), ''), NULLIF(TRIM(p_unit_of_measure), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_parts_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_update;

CREATE PROCEDURE sp_mock_parts_update(
    IN p_id BIGINT,
    IN p_part_number VARCHAR(50),
    IN p_part_category VARCHAR(32),
    IN p_part_description VARCHAR(500),
    IN p_unit_of_measure VARCHAR(20)
)
UPDATE mock_parts
SET part_number = NULLIF(TRIM(p_part_number), ''), part_category = NULLIF(TRIM(p_part_category), ''), part_description = NULLIF(TRIM(p_part_description), ''), unit_of_measure = NULLIF(TRIM(p_unit_of_measure), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_request_types_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_delete;

CREATE PROCEDURE sp_mock_request_types_delete(IN p_id BIGINT)
DELETE FROM mock_request_types WHERE id = p_id;


-- Create procedure: sp_mock_request_types_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_get;

CREATE PROCEDURE sp_mock_request_types_get()
SELECT id, public_id, request_type, subtype, is_active, created_utc, updated_utc
FROM mock_request_types
ORDER BY id ASC;


-- Create procedure: sp_mock_request_types_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_insert;

CREATE PROCEDURE sp_mock_request_types_insert(
    IN p_request_type VARCHAR(64),
    IN p_subtype VARCHAR(128)
)
INSERT INTO mock_request_types (request_type, subtype, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_request_type), ''), NULLIF(TRIM(p_subtype), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_request_types_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_update;

CREATE PROCEDURE sp_mock_request_types_update(
    IN p_id BIGINT,
    IN p_request_type VARCHAR(64),
    IN p_subtype VARCHAR(128)
)
UPDATE mock_request_types
SET request_type = NULLIF(TRIM(p_request_type), ''), subtype = NULLIF(TRIM(p_subtype), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_requesters_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_delete;

CREATE PROCEDURE sp_mock_requesters_delete(IN p_id BIGINT)
DELETE FROM mock_requesters WHERE id = p_id;


-- Create procedure: sp_mock_requesters_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_get;

CREATE PROCEDURE sp_mock_requesters_get()
SELECT id, public_id, employee_number, display_name, is_active, created_utc, updated_utc
FROM mock_requesters
ORDER BY id ASC;


-- Create procedure: sp_mock_requesters_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_insert;

CREATE PROCEDURE sp_mock_requesters_insert(
    IN p_employee_number VARCHAR(32),
    IN p_display_name VARCHAR(128)
)
INSERT INTO mock_requesters (employee_number, display_name, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_employee_number), ''), NULLIF(TRIM(p_display_name), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_requesters_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_update;

CREATE PROCEDURE sp_mock_requesters_update(
    IN p_id BIGINT,
    IN p_employee_number VARCHAR(32),
    IN p_display_name VARCHAR(128)
)
UPDATE mock_requesters
SET employee_number = NULLIF(TRIM(p_employee_number), ''), display_name = NULLIF(TRIM(p_display_name), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_work_centers_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_delete;

CREATE PROCEDURE sp_mock_work_centers_delete(IN p_id BIGINT)
DELETE FROM mock_work_centers WHERE id = p_id;


-- Create procedure: sp_mock_work_centers_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_get;

CREATE PROCEDURE sp_mock_work_centers_get()
SELECT id, public_id, work_center_code, work_center_description, building, is_active, created_utc, updated_utc
FROM mock_work_centers
ORDER BY id ASC;


-- Create procedure: sp_mock_work_centers_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_insert;

CREATE PROCEDURE sp_mock_work_centers_insert(
    IN p_work_center_code VARCHAR(32),
    IN p_work_center_description VARCHAR(255),
    IN p_building VARCHAR(128)
)
INSERT INTO mock_work_centers (work_center_code, work_center_description, building, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_work_center_code), ''), NULLIF(TRIM(p_work_center_description), ''), NULLIF(TRIM(p_building), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_work_centers_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_update;

CREATE PROCEDURE sp_mock_work_centers_update(
    IN p_id BIGINT,
    IN p_work_center_code VARCHAR(32),
    IN p_work_center_description VARCHAR(255),
    IN p_building VARCHAR(128)
)
UPDATE mock_work_centers
SET work_center_code = NULLIF(TRIM(p_work_center_code), ''), work_center_description = NULLIF(TRIM(p_work_center_description), ''), building = NULLIF(TRIM(p_building), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_mock_work_orders_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_delete;

CREATE PROCEDURE sp_mock_work_orders_delete(IN p_id BIGINT)
DELETE FROM mock_work_orders WHERE id = p_id;


-- Create procedure: sp_mock_work_orders_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_get;

CREATE PROCEDURE sp_mock_work_orders_get()
SELECT id, public_id, work_order_number, part_number, work_center_code, is_coil_bearing, is_active, created_utc, updated_utc
FROM mock_work_orders
ORDER BY id ASC;


-- Create procedure: sp_mock_work_orders_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_insert;

CREATE PROCEDURE sp_mock_work_orders_insert(
    IN p_work_order_number VARCHAR(64),
    IN p_part_number VARCHAR(50),
    IN p_work_center_code VARCHAR(32),
    IN p_is_coil_bearing TINYINT
)
INSERT INTO mock_work_orders (work_order_number, part_number, work_center_code, is_coil_bearing, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_work_order_number), ''), NULLIF(TRIM(p_part_number), ''), NULLIF(TRIM(p_work_center_code), ''), p_is_coil_bearing, UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());


-- Create procedure: sp_mock_work_orders_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_update;

CREATE PROCEDURE sp_mock_work_orders_update(
    IN p_id BIGINT,
    IN p_work_order_number VARCHAR(64),
    IN p_part_number VARCHAR(50),
    IN p_work_center_code VARCHAR(32),
    IN p_is_coil_bearing TINYINT
)
UPDATE mock_work_orders
SET work_order_number = NULLIF(TRIM(p_work_order_number), ''), part_number = NULLIF(TRIM(p_part_number), ''), work_center_code = NULLIF(TRIM(p_work_center_code), ''), is_coil_bearing = p_is_coil_bearing, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


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


-- Create procedure: sp_waitlist_request_types_insert
-- Engine: MySQL 5.7
-- Purpose: Insert a real (non-mock) request type into waitlist_request_types.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_insert;

CREATE PROCEDURE sp_waitlist_request_types_insert(
    IN p_request_type VARCHAR(64),
    IN p_control VARCHAR(255),
    IN p_flow VARCHAR(64),
    IN p_requires_text_input TINYINT,
    IN p_prompt_text VARCHAR(500),
    IN p_min_length INT,
    IN p_max_length INT,
    IN p_default_image_path VARCHAR(500),
    IN p_center_data_grid_fields_json JSON,
    IN p_is_active TINYINT
)
INSERT INTO waitlist_request_types (
    public_id, request_type, control, flow, requires_text_input, prompt_text,
    min_length, max_length, default_image_path, center_data_grid_fields_json,
    is_active, created_utc, updated_utc
)
VALUES (
    UUID(),
    NULLIF(TRIM(p_request_type), ''),
    NULLIF(TRIM(p_control), ''),
    NULLIF(TRIM(COALESCE(p_flow, 'direct-to-confirmation')), ''),
    COALESCE(p_requires_text_input, 0),
    NULLIF(TRIM(COALESCE(p_prompt_text, '')), ''),
    COALESCE(p_min_length, 0),
    COALESCE(p_max_length, 200),
    NULLIF(TRIM(COALESCE(p_default_image_path, '')), ''),
    p_center_data_grid_fields_json,
    COALESCE(p_is_active, 1),
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);


-- Create procedure: sp_waitlist_request_types_update
-- Engine: MySQL 5.7
-- Purpose: Update a real (non-mock) request type in waitlist_request_types by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_update;

CREATE PROCEDURE sp_waitlist_request_types_update(
    IN p_id BIGINT,
    IN p_request_type VARCHAR(64),
    IN p_control VARCHAR(255),
    IN p_flow VARCHAR(64),
    IN p_requires_text_input TINYINT,
    IN p_prompt_text VARCHAR(500),
    IN p_min_length INT,
    IN p_max_length INT,
    IN p_default_image_path VARCHAR(500),
    IN p_center_data_grid_fields_json JSON,
    IN p_is_active TINYINT
)
UPDATE waitlist_request_types
SET request_type = NULLIF(TRIM(p_request_type), ''),
    control = NULLIF(TRIM(p_control), ''),
    flow = NULLIF(TRIM(COALESCE(p_flow, 'direct-to-confirmation')), ''),
    requires_text_input = COALESCE(p_requires_text_input, 0),
    prompt_text = NULLIF(TRIM(COALESCE(p_prompt_text, '')), ''),
    min_length = COALESCE(p_min_length, 0),
    max_length = COALESCE(p_max_length, 200),
    default_image_path = NULLIF(TRIM(COALESCE(p_default_image_path, '')), ''),
    center_data_grid_fields_json = p_center_data_grid_fields_json,
    is_active = COALESCE(p_is_active, 1),
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_waitlist_request_types_delete
-- Engine: MySQL 5.7
-- Purpose: Delete a real (non-mock) request type and its subtypes by type id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_delete;

CREATE PROCEDURE sp_waitlist_request_types_delete(IN p_id BIGINT)
DELETE waitlist_request_subtypes, waitlist_request_types
FROM waitlist_request_types
LEFT JOIN waitlist_request_subtypes ON waitlist_request_subtypes.request_type_id = waitlist_request_types.id
WHERE waitlist_request_types.id = p_id;


-- Create procedure: sp_waitlist_request_subtypes_insert
-- Engine: MySQL 5.7
-- Purpose: Insert a real (non-mock) request subtype into waitlist_request_subtypes.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_insert;

CREATE PROCEDURE sp_waitlist_request_subtypes_insert(
    IN p_request_type_id BIGINT,
    IN p_subtype_name VARCHAR(128),
    IN p_control VARCHAR(255),
    IN p_flow VARCHAR(64),
    IN p_requires_text_input TINYINT,
    IN p_prompt_text VARCHAR(500),
    IN p_min_length INT,
    IN p_max_length INT,
    IN p_default_image_path VARCHAR(500),
    IN p_center_data_grid_fields_json JSON,
    IN p_is_active TINYINT
)
INSERT INTO waitlist_request_subtypes (
    public_id, request_type_id, subtype_name, control, flow, requires_text_input, prompt_text,
    min_length, max_length, default_image_path, center_data_grid_fields_json,
    is_active, created_utc, updated_utc
)
VALUES (
    UUID(),
    p_request_type_id,
    NULLIF(TRIM(p_subtype_name), ''),
    NULLIF(TRIM(p_control), ''),
    NULLIF(TRIM(COALESCE(p_flow, 'direct-to-confirmation')), ''),
    COALESCE(p_requires_text_input, 0),
    NULLIF(TRIM(COALESCE(p_prompt_text, '')), ''),
    COALESCE(p_min_length, 0),
    COALESCE(p_max_length, 200),
    NULLIF(TRIM(COALESCE(p_default_image_path, '')), ''),
    p_center_data_grid_fields_json,
    COALESCE(p_is_active, 1),
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);


-- Create procedure: sp_waitlist_request_subtypes_update
-- Engine: MySQL 5.7
-- Purpose: Update a real (non-mock) request subtype in waitlist_request_subtypes by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_update;

CREATE PROCEDURE sp_waitlist_request_subtypes_update(
    IN p_id BIGINT,
    IN p_request_type_id BIGINT,
    IN p_subtype_name VARCHAR(128),
    IN p_control VARCHAR(255),
    IN p_flow VARCHAR(64),
    IN p_requires_text_input TINYINT,
    IN p_prompt_text VARCHAR(500),
    IN p_min_length INT,
    IN p_max_length INT,
    IN p_default_image_path VARCHAR(500),
    IN p_center_data_grid_fields_json JSON,
    IN p_is_active TINYINT
)
UPDATE waitlist_request_subtypes
SET request_type_id = p_request_type_id,
    subtype_name = NULLIF(TRIM(p_subtype_name), ''),
    control = NULLIF(TRIM(p_control), ''),
    flow = NULLIF(TRIM(COALESCE(p_flow, 'direct-to-confirmation')), ''),
    requires_text_input = COALESCE(p_requires_text_input, 0),
    prompt_text = NULLIF(TRIM(COALESCE(p_prompt_text, '')), ''),
    min_length = COALESCE(p_min_length, 0),
    max_length = COALESCE(p_max_length, 200),
    default_image_path = NULLIF(TRIM(COALESCE(p_default_image_path, '')), ''),
    center_data_grid_fields_json = p_center_data_grid_fields_json,
    is_active = COALESCE(p_is_active, 1),
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;


-- Create procedure: sp_waitlist_request_subtypes_delete
-- Engine: MySQL 5.7
-- Purpose: Delete a real (non-mock) request subtype by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_delete;

CREATE PROCEDURE sp_waitlist_request_subtypes_delete(IN p_id BIGINT)
DELETE FROM waitlist_request_subtypes WHERE id = p_id;


-- Create procedure: sp_mock_master_table_columns_get
-- Engine: MySQL 5.7
-- Purpose: Return the editable-column metadata for a Developer-editable mock master table so the
--          editor can render an appropriate grid. Column names come from information_schema and are
--          never interpolated; the table name is validated against the known mock tables first.
-- NOTE: This SP does NOT cover the real (non-mock) request-type/subtype tables; they have their own
--       dedicated SPs + real-catalog service.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_table_columns_get;

CREATE PROCEDURE sp_mock_master_table_columns_get(IN p_table_name VARCHAR(128))
SELECT
    c.COLUMN_NAME AS column_name,
    c.DATA_TYPE   AS data_type,
    c.IS_NULLABLE AS is_nullable,
    CASE WHEN c.COLUMN_NAME IN ('id','public_id','created_utc','updated_utc') THEN 1 ELSE 0 END AS is_audit
FROM information_schema.COLUMNS c
JOIN mock_master_tables_registry r
  ON r.table_name = c.TABLE_NAME
 AND r.is_active = 1
WHERE c.TABLE_SCHEMA = DATABASE()
  AND c.TABLE_NAME = TRIM(p_table_name)
ORDER BY c.ORDINAL_POSITION;

-- Create procedure: sp_waitlist_request_types_get_all
-- Engine: MySQL 5.7
-- Purpose: Return ALL real (non-mock) request-type catalog rows INCLUDING inactive ones, for the
--          Developer/Admin editor (so an inactive type can be viewed, edited, and re-activated).
--          The wizard uses sp_waitlist_request_types_get (active only); this is the admin variant.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_get_all;

CREATE PROCEDURE sp_waitlist_request_types_get_all()
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
    is_active,
    created_utc,
    updated_utc
FROM waitlist_request_types
ORDER BY id ASC;

-- Create procedure: sp_waitlist_request_subtypes_get_all
-- Engine: MySQL 5.7
-- Purpose: Return ALL real (non-mock) request-subtype catalog rows INCLUDING inactive ones, for the
--          Developer/Admin editor (so an inactive subtype can be viewed, edited, and re-activated).
--          The wizard uses sp_waitlist_request_subtypes_get (active only); this is the admin variant.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_get_all;

CREATE PROCEDURE sp_waitlist_request_subtypes_get_all()
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
    is_active,
    created_utc,
    updated_utc
FROM waitlist_request_subtypes
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
