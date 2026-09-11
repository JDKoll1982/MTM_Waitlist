-- Stored Procedure: sp_config_dunnage_types_visibility_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
--
-- Purpose: the persisted dunnage-type visibility map, so the catalog can split types into visible and hidden.
--          Replaces the inline SELECT in `DunnageTypeVisibilityCatalogService.GetVisibilityMapAsync` (FR-015).
--
-- Contract:
--   OUT one row per stored type: dunnage_type_id, is_visible.
--
-- No filter and no ordering, exactly like the statement this replaces: the caller folds the rows into a
-- dictionary, and a type with no row is visible by default (the service treats a missing entry as visible).
-- Adding an `is_visible = 1` filter here would silently change that default for every stored-hidden type.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_get;

CREATE PROCEDURE sp_config_dunnage_types_visibility_get()
SELECT dunnage_type_id, is_visible
FROM config_dunnage_types_visibility;
