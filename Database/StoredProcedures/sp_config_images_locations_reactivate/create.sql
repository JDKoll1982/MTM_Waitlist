-- Stored Procedure: sp_config_images_locations_reactivate
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: bring a soft-deleted override back to life with a new image path. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.ReactivateOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_image_path     VARCHAR(500)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count; the caller treats less than 1 as a failure, which is also what it did before.
--
-- **No `is_active` predicate in the WHERE, matching the statement this replaces.** It reads as an omission but it
-- is not: the caller only reaches this path after `sp_config_images_locations_status_get` reported an existing
-- row whose `is_active` is 0, and the unique key guarantees that pair has exactly one row, so the update is
-- already aimed at that one row. Adding `AND is_active = 0` would be a tighter guard, but it would make the
-- procedure fail if the row had been flipped to active between the probe and this call, turning a harmless race
-- into a reported error; the current form is idempotent in that case, which is why it was written this way.
--
-- `created_utc`/`created_by_user_id` are left alone — this is the same logical row, so its creation record stays.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_reactivate;

CREATE PROCEDURE sp_config_images_locations_reactivate(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    is_active = 1,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id;
