-- Create procedure: sp_waitlist_request_get
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: a request is identified by the Category and the Item the
-- requester chose, and the two legacy columns this procedure used to return no longer exist (FR-004).

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
    category,
    item,
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
