-- Create procedure: sp_waitlist_request_status_update
-- Engine: MySQL 5.7
-- The transition is CONDITIONAL on the state the caller read (FR-043). Two handlers acting on the same request
-- at the same moment must produce exactly one outcome: the second handler's accept matches no row, the update
-- reports zero affected rows, and the caller is told rather than shown a success it did not get. Matching on
-- public_id alone is what let a second accept overwrite the first handler's assignment.
-- p_expected_status is the state the caller saw. An absent or empty value keeps the unconditional behaviour for
-- a caller that genuinely has no state to guard on.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_status_update;

CREATE PROCEDURE sp_waitlist_request_status_update(
    IN p_public_id CHAR(36),
    IN p_status VARCHAR(32),
    IN p_assigned_material_handler VARCHAR(128),
    IN p_cancellation_reason VARCHAR(255),
    IN p_canceled_by_employee_number VARCHAR(32),
    IN p_note VARCHAR(500),
    IN p_expected_status VARCHAR(32)
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
WHERE public_id = p_public_id
  AND (
      NULLIF(TRIM(COALESCE(p_expected_status, '')), '') IS NULL
      OR UPPER(TRIM(status)) = UPPER(TRIM(p_expected_status) COLLATE utf8mb4_unicode_ci)
  );
