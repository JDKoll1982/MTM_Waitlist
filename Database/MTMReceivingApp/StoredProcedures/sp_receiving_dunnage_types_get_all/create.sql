-- Stored Procedure: sp_receiving_dunnage_types_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
-- Database: mtm_receiving_application
--
-- Purpose: the dunnage types the visibility editor lists — every type that has a usable name, alphabetically.
--          Replaces the inline SELECT in `DunnageTypeVisibilityCatalogService.GetAllDunnageTypesAsync`, which
--          reads this store directly (FR-015).
--
-- Why this is a new artifact and not the existing `sp_Dunnage_Types_GetAll`
-- ----------------------------------------------------------------------
-- That procedure was checked against its live definition before being considered, and it is **not** equivalent:
-- it is `SELECT * FROM dunnage_types ORDER BY id`, so it
--   * includes rows whose `type_name` is NULL or blank, which this caller deliberately excludes (a blank name
--     cannot be shown as a visibility option), and
--   * orders by `id`, while this caller orders by `type_name` so the list reads alphabetically.
-- It also returns every column, including ones this caller does not map. Routing to it would have changed both
-- the content and the order of the list, so the caller's own read was preserved here instead. The existing
-- procedure is left exactly as it is: the MTM_Receiving_Application's own screens still call it.
--
-- Contract:
--   OUT one row per usable type: id, type_name — same two columns and same ordering as the statement it replaces.
-- ============================================================

USE mtm_receiving_application;

DROP PROCEDURE IF EXISTS sp_receiving_dunnage_types_get_all;

CREATE PROCEDURE sp_receiving_dunnage_types_get_all()
SELECT id, type_name
FROM dunnage_types
WHERE type_name IS NOT NULL
    AND TRIM(type_name) <> ''
ORDER BY type_name ASC;
