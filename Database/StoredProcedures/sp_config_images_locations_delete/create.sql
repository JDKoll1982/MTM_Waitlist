-- Stored Procedure: sp_config_images_locations_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: withdraw the override for one (scope, scope_item_id) pair. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.DeleteOverrideAsync` (FR-015). This is a soft delete: the row survives
--          because `uq_config_images_locations_scope_item` spans the pair regardless of `is_active`, so a hard
--          delete would let the next create re-use the pair while an audit trail of the old image is lost.
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count; 0 is the caller's NOT_FOUND answer, exactly as before.
--
-- `AND is_active = 1` is kept from the statement this replaces: withdrawing an already-withdrawn override is not
-- a state change, so it must not report success. That also means the returned count is a genuine "did something
-- change" signal rather than "did a row match".
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete;

CREATE PROCEDURE sp_config_images_locations_delete(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;
