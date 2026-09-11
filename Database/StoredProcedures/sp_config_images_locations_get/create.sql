-- Stored Procedure: sp_config_images_locations_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: the active image override for one exact (scope, scope_item_id) pair. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)    column width from config_images_locations
--   IN  p_scope_item_id  VARCHAR(190)
--   OUT zero or one row, the ten columns the caller maps (id, public_id, scope, scope_item_id, image_path,
--       is_active, created_by_user_id, updated_by_user_id, created_utc, updated_utc)
--
-- `is_active = 1` is part of the identity, not a filter the caller could add later: the override table is
-- soft-deleted, so a row whose `is_active` is 0 is a withdrawn override and must never be returned here. No row
-- is the caller's normal "no override" answer, which is why the procedure returns nothing rather than a null row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get;

CREATE PROCEDURE sp_config_images_locations_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
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
  AND scope_item_id = p_scope_item_id
  AND is_active = 1
LIMIT 1;
