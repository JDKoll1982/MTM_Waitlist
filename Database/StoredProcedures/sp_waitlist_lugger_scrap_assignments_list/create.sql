-- Stored Procedure: sp_waitlist_lugger_scrap_assignments_list
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T018)
--
-- Purpose: read one scrap type's luggers in the order the system tries them, each carrying the lugger's current
--          status, so the editor can show the try order and the blocked reason without a second call.
--
-- Contract:
--   IN  p_scrap_type_key VARCHAR(128) the normalised scrap type to read
--
--   OUT id, public_id, scrap_type, scrap_type_key, lugger_public_id, lugger_name, status, status_reason,
--       is_dump_point, sort_position
--
-- Guarantee: one row per assignment of that scrap type, in `sort_position` then `lugger_name` order, with the
--            status read live from the lugger rather than copied onto the pairing (research D2). A scrap type
--            nobody is assigned to returns no row, which is how the caller tells "no lugger for this type" from
--            "the read failed" (FR-006, FR-026, FR-031).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_scrap_assignments_list;

CREATE PROCEDURE sp_waitlist_lugger_scrap_assignments_list(
    IN p_scrap_type_key VARCHAR(128)
)
SELECT
    a.id,
    a.public_id,
    a.scrap_type,
    a.scrap_type_key,
    l.public_id AS lugger_public_id,
    l.lugger_name,
    l.status,
    l.status_reason,
    a.is_dump_point,
    a.sort_position
FROM
    waitlist_lugger_scrap_assignments AS a
    INNER JOIN waitlist_lugger_locations AS l ON l.id = a.lugger_id
WHERE
    a.scrap_type_key = LOWER(TRIM(p_scrap_type_key))
ORDER BY
    a.sort_position ASC,
    l.lugger_name ASC;
