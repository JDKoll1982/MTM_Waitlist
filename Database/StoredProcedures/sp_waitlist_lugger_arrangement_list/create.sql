-- Stored Procedure: sp_waitlist_lugger_arrangement_list
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T021)
--
-- Purpose: the one read behind the arrangement summary, the arrangement editor and the card path, so a list
--          load costs one call for the whole arrangement rather than one call per lugger (FR-030, FR-031,
--          research D1).
--
-- Contract:
--   IN  none
--
--   OUT zero or more rows, ordered by lugger_display_position ascending, then assignment_sort_position
--       ascending, then lugger_name ascending, in exactly this column order:
--         lugger_public_id, lugger_name, status, status_reason, lugger_display_position,
--         scrap_type, scrap_type_key, assignment_public_id, is_dump_point, assignment_sort_position
--
-- Guarantee: one row per lugger and assignment pairing, and a lugger that takes no scrap type appears once with
--            `scrap_type`, `scrap_type_key`, `assignment_public_id`, `is_dump_point` and
--            `assignment_sort_position` all null, which is what the editor draws its empty lugger cell from
--            (FR-026). The LEFT JOIN is what makes that single row possible: an inner join would silently drop
--            every lugger with no assignment, so the summary would lose a lugger the lead had just added.
--
-- The read is not a transaction. It takes no locks and writes nothing, so it can never be the call that blocks
-- a lead mid-edit.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_arrangement_list;

CREATE PROCEDURE sp_waitlist_lugger_arrangement_list()
SELECT l.public_id AS lugger_public_id,
       l.lugger_name AS lugger_name,
       l.status AS status,
       l.status_reason AS status_reason,
       l.display_position AS lugger_display_position,
       a.scrap_type AS scrap_type,
       a.scrap_type_key AS scrap_type_key,
       a.public_id AS assignment_public_id,
       a.is_dump_point AS is_dump_point,
       a.sort_position AS assignment_sort_position
FROM waitlist_lugger_locations l
LEFT JOIN waitlist_lugger_scrap_assignments a ON a.lugger_id = l.id
ORDER BY l.display_position ASC,
         a.sort_position ASC,
         l.lugger_name ASC;
