-- Stored Procedure: sp_config_images_locations_count_by_scope_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: how many active overrides one scope holds. Replaces the inline `SELECT COUNT(*)` in
--          `ImageOverrideReadService.CountActiveOverridesByScopeAsync` (FR-015).
--
-- Contract:
--   IN  p_scope  VARCHAR(16)
--   OUT one row / one column: `count` — the alias the caller reads by name.
--
-- Same predicate as the statement it replaces (`scope` + `is_active = 1`), so the per-scope counters in the
-- Settings UI keep the values they had.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_count_by_scope_get;

CREATE PROCEDURE sp_config_images_locations_count_by_scope_get(
    IN p_scope VARCHAR(16)
)
SELECT COUNT(*) as count
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1;
