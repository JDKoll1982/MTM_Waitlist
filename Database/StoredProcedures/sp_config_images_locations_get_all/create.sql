-- Stored Procedure: sp_config_images_locations_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: every active override, whatever its scope. Replaces the inline SELECT in
--          `ImageOverrideReadService.DetectOrphanedOverridesAsync` (FR-015).
--
-- Contract:
--   OUT one row per active override (the same ten columns). No ordering, deliberately: the caller checks each row
--       against its scope's own source table and collects the orphans, so the order it receives is irrelevant and
--       adding one here would only be a difference to explain later.
--
-- The caller is the orphan detector, which is why the active filter is the same `is_active = 1` the statement used
-- and why nothing else is narrowed: an orphan is exactly an active override whose scope item no longer exists.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_get_all;

CREATE PROCEDURE sp_config_images_locations_get_all()
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
WHERE is_active = 1;
