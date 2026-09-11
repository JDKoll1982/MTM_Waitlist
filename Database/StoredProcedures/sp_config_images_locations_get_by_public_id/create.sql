-- Stored Procedure: sp_config_images_locations_get_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: one active override, addressed by its public identifier. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetOverrideByPublicIdAsync` (FR-015).
--
-- Contract:
--   IN  p_public_id  CHAR(36)     column width from config_images_locations.public_id
--   OUT zero or one row (the same ten columns); no row is the caller's "not found" answer.
--
-- `is_active = 1` matches the statement this replaces: a withdrawn override is not addressable by its public id,
-- and the Settings deep-link that uses this read must not resurrect one.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_by_public_id;

CREATE PROCEDURE sp_config_images_locations_get_by_public_id(
    IN p_public_id CHAR(36)
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
WHERE public_id = p_public_id
  AND is_active = 1
LIMIT 1;
