-- Create procedure: sp_waitlist_request_handler_status_update
-- Engine: MySQL 5.7
-- The write is CONDITIONAL on the handler the caller read (FR-070, FR-073, FR-076). Two handlers acting on the
-- same request at the same moment must produce exactly one outcome: the second caller matches no row, the update
-- reports zero affected rows, and the caller is told rather than shown a success it did not get. Matching on
-- public_id alone would let a caller that is not the row's own material handler set a condition on somebody
-- else's work, which is the same lost-race hole sp_waitlist_request_status_update closes with p_expected_status.
-- p_expected_material_handler is the handler the caller saw, and it is what decides the match. Unlike that
-- sibling's p_expected_status, an absent or empty value is NOT treated as "unconditional": a handler gate with
-- nothing to match on leaves the row alone and reports 0, because the one thing this procedure must never do is
-- write a handler status for somebody who is not that handler.
-- The return value is the UPDATE's own affected-row count, the same way sp_waitlist_request_status_update
-- reports it, so the caller's write seam can tell a lost race from a write.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_handler_status_update;

CREATE PROCEDURE sp_waitlist_request_handler_status_update(
    IN p_public_id CHAR(36),
    IN p_handler_status VARCHAR(32),
    IN p_expected_material_handler VARCHAR(128)
)
UPDATE waitlist_requests_queue
SET handler_status = NULLIF(TRIM(COALESCE(p_handler_status, '')) COLLATE utf8mb4_unicode_ci, ''),
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND NULLIF(TRIM(COALESCE(p_expected_material_handler, '')), '') IS NOT NULL
  AND assigned_material_handler = TRIM(p_expected_material_handler);
