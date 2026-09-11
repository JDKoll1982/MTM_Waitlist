-- Stored Procedure: sp_config_images_locations_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: repoint one live override at a different image. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.UpdateOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_image_path     VARCHAR(500)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count.
--
-- `AND is_active = 1` is kept from the statement this replaces: an update is only meaningful for a live override,
-- and the caller has already reported NOT_FOUND when it could not read one. `created_*` columns are untouched —
-- repointing an image is not a new row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_update;

CREATE PROCEDURE sp_config_images_locations_update(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;
