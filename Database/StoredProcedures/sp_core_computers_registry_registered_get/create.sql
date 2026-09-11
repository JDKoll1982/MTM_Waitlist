-- Stored Procedure: sp_core_computers_registry_registered_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092; found while routing WorkCenterCatalogService)
--
-- Purpose: the registered machines, in display order — the computer picker's source. Replaces the inline
--          SELECT in `WorkCenterCatalogService.GetAvailableComputersAsync` (FR-015).
--
-- Why this is an extra artifact, and why it is not `sp_core_computers_registry_get_all`: the two reads differ
-- by exactly one predicate. `get_all` returns every row (it serves the registry editor, which must be able to
-- show a machine an operator has retired); this one returns only `is_registered = 1` (it serves a picker, which
-- must not offer a retired machine). Routing the picker to `get_all` would have quietly added unregistered
-- machines to it the first time an operator retired one, so the filter is kept in the database as its own
-- procedure instead of being dropped or duplicated into C#.
--
-- Contract:
--   OUT one row per registered machine: computer_name, display_name — ordered by display_name then
--       computer_name, the same order the statement this replaces used.
--
-- No parameters: "registered" is the procedure's whole job.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_registered_get;

CREATE PROCEDURE sp_core_computers_registry_registered_get()
SELECT
    computer_name,
    display_name
FROM core_computers_registry
WHERE is_registered = 1
ORDER BY display_name ASC, computer_name ASC;
