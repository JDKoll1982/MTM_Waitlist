-- Create procedure: sp_waitlist_request_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

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
