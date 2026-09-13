-- Create procedure: sp_waitlist_request_audit_list
-- Engine: MySQL 5.7
-- Purpose: Read one request's persisted audit/history entries, oldest first, so the request page can show
--          the history that was recorded before this session. The write half is sp_waitlist_request_audit_insert.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_audit_list;

CREATE PROCEDURE sp_waitlist_request_audit_list(
    IN p_request_public_id CHAR(36)
)
SELECT
    id,
    public_id,
    request_public_id,
    from_status,
    to_status,
    event_type,
    actor_employee_number,
    actor_employee_name,
    details,
    occurred_utc
FROM waitlist_requests_audit
WHERE request_public_id = TRIM(p_request_public_id)
ORDER BY occurred_utc ASC, id ASC;
