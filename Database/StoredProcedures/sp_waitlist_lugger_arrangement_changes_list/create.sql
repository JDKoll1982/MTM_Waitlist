-- Stored Procedure: sp_waitlist_lugger_arrangement_changes_list
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T022)
--
-- Purpose: hand the caller the arrangement changes it has not seen yet, and hand it its starting watermark in
--          the same shape, so a change made before the list opened is never replayed as though it had just
--          happened (FR-033, FR-037).
--
-- Contract:
--   IN  p_after_id BIGINT  the highest change id the caller has already seen, or NULL to take a watermark
--
--   OUT id, public_id, change_kind, lugger_name, scrap_type, previous_value, new_value, change_reason,
--       changed_by_employee_name, changed_utc
--
-- Guarantee: with a value in p_after_id, only rows whose `id` is greater are returned, in `id` order, so the
--            caller's cursor advances and nothing is skipped or replayed. With a NULL p_after_id, exactly one
--            row is returned whose `id` is the current highest change id (0 when no change has been recorded)
--            and whose every other column is NULL. That row is the watermark and nothing else: it is never a
--            change, so a caller cannot mistake it for one.
--
-- The watermark is read from the same table the poll reads, under one statement, so the watermark a caller takes
-- is a value that already exists on disk rather than a guess between two reads.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_arrangement_changes_list;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_waitlist_lugger_arrangement_changes_list(
    IN p_after_id BIGINT
)
BEGIN
    IF p_after_id IS NULL THEN
        SELECT
            COALESCE(MAX(c.id), 0) AS id,
            NULL AS public_id,
            NULL AS change_kind,
            NULL AS lugger_name,
            NULL AS scrap_type,
            NULL AS previous_value,
            NULL AS new_value,
            NULL AS change_reason,
            NULL AS changed_by_employee_name,
            NULL AS changed_utc
        FROM
            waitlist_lugger_arrangement_changes AS c;
    ELSE
        SELECT
            c.id,
            c.public_id,
            c.change_kind,
            c.lugger_name,
            c.scrap_type,
            c.previous_value,
            c.new_value,
            c.change_reason,
            c.changed_by_employee_name,
            c.changed_utc
        FROM
            waitlist_lugger_arrangement_changes AS c
        WHERE
            c.id > p_after_id
        ORDER BY
            c.id ASC;
    END IF;
END$$

DELIMITER ;
