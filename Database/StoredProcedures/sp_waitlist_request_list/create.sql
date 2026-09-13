-- Create procedure: sp_waitlist_request_list
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_list;

-- last_message_utc is the newest NoteUpdated entry written by a person, and it is what the card's
-- new-message marker compares against. The request's own updated_utc cannot serve that purpose: it moves
-- for every lifecycle change, so a system event (created, accepted, completed, canceled) would raise the
-- marker as if somebody had sent a message. Entries with no actor are system writes and are excluded.
CREATE PROCEDURE sp_waitlist_request_list(
    IN p_building VARCHAR(64),
    IN p_include_resolved TINYINT
)
SELECT
    q.id,
    q.public_id,
    q.building,
    q.work_center,
    q.request_type,
    q.subtype,
    q.input_value,
    q.active_setup_job_id,
    q.work_center_name,
    q.requester_employee_number,
    q.requester_employee_name,
    q.status,
    q.requested_utc,
    q.target_time_utc,
    q.is_overdue,
    q.assigned_material_handler,
    q.cancellation_reason,
    q.canceled_utc,
    q.canceled_by_employee_number,
    q.note,
    q.accepted_utc,
    q.completed_utc,
    q.released_utc,
    q.created_utc,
    q.updated_utc,
    (SELECT MAX(a.occurred_utc)
       FROM waitlist_requests_audit a
      WHERE a.request_public_id = q.public_id
        AND a.event_type = 'NoteUpdated'
        AND a.actor_employee_number IS NOT NULL) AS last_message_utc
FROM waitlist_requests_queue q
WHERE (p_building IS NULL OR TRIM(p_building) = '' OR q.building = TRIM(p_building))
  AND (p_include_resolved = 1 OR q.status IN ('Pending', 'Accepted'))
ORDER BY q.requested_utc ASC;
