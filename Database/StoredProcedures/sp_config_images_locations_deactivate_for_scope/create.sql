-- Stored Procedure: sp_config_images_locations_deactivate_for_scope
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: withdraw every live override in one scope at once. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.DeactivateAllForScopeAsync` (FR-015).
--
-- Contract:
--   IN  p_scope    VARCHAR(16)
--   IN  p_user_id  BIGINT     NULL when the caller has no user
--   OUT affected row count — the number of overrides deactivated, which the caller returns to the UI.
--
-- **The return value is why this site had to change.** The caller previously ran this UPDATE through the
-- row-returning query helper and returned `rows.Count`; an UPDATE returns no rows, so the count was always 0 and
-- the UI reported "0 deactivated" however many overrides were withdrawn. The non-query path carries the real
-- count. Because the WHERE clause matches only `is_active = 1` rows and changes every matched row, the count is
-- the same as the matched-row count — no `UseAffectedRows` subtlety to worry about here.
--
-- Soft delete, like the single-row withdraw: this is a bulk version of `sp_config_images_locations_delete`, not a
-- delete, which is why it does not share `sp_config_images_locations_purge_inactive`'s hard-delete behaviour.
-- `AND is_active = 1` is kept from the statement this replaces, so an already-withdrawn override is not counted.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_deactivate_for_scope;

CREATE PROCEDURE sp_config_images_locations_deactivate_for_scope(
    IN p_scope VARCHAR(16),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND is_active = 1;
