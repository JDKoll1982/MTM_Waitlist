-- Stored Procedure: sp_config_images_locations_get_by_scope
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: every active override in one scope, newest first. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetOverridesByScopeAsync` (FR-015).
--
-- Contract:
--   IN  p_scope  VARCHAR(16)
--   OUT one row per active override (same ten columns), ordered `updated_utc DESC` — the order the statement this
--       replaces used, which the Settings list depends on for "most recently changed first".
--
-- Same `is_active = 1` reasoning as `sp_config_images_locations_get`: withdrawn overrides are not part of the
-- scope's live set.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_by_scope;

CREATE PROCEDURE sp_config_images_locations_get_by_scope(
    IN p_scope VARCHAR(16)
)
SELECT
    id,
    public_id,
    scope,
    scope_item_id,
    image_path,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM config_images_locations
WHERE scope = p_scope
  AND is_active = 1
ORDER BY updated_utc DESC;
