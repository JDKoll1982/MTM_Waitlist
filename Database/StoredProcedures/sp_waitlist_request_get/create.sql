-- Create procedure: sp_waitlist_request_get
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: a request is identified by the Category and the Item the
-- requester chose, and the two legacy columns this procedure used to return no longer exist (FR-004).
-- Extended for FR-022: the row also carries the material handler's display name, so a reader never has to
-- print the employee number the queue stores to say who holds the request.

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
    -- The handler's display name, resolved from the entry the handler themselves wrote when they took the
    -- request. See sp_waitlist_request_list for why the name comes from there and not from this table.
    (SELECT a.actor_employee_name
       FROM waitlist_requests_audit a
      WHERE a.request_public_id = waitlist_requests_queue.public_id
        AND a.actor_employee_name IS NOT NULL
        AND a.actor_employee_number = waitlist_requests_queue.assigned_material_handler
      ORDER BY a.occurred_utc DESC
      LIMIT 1) AS assigned_material_handler_name,
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
