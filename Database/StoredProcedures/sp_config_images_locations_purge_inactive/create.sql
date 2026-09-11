-- Stored Procedure: sp_config_images_locations_purge_inactive
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: permanently remove every withdrawn override. Replaces the inline `DELETE FROM … WHERE is_active = 0`
--          in `ImageOverrideWriteService.PurgeInactiveOverridesAsync` (FR-015). This is the only hard delete on
--          the table, and it is the deliberate escape hatch for the soft-delete design
--          (`sp_config_images_locations_delete` keeps the row; this is how an operator ultimately reclaims it).
--
-- Contract:
--   OUT affected row count — the number of rows actually purged. The caller returns this count to the UI, which
--       reports it, so a wrong number is a user-visible lie.
--
-- **The return value is why this site had to change.** The caller previously ran this DELETE through the
-- row-returning query helper and returned `rows.Count`. A DELETE returns no rows, so the count was always 0 —
-- the purge worked, but every caller was told nothing had been purged. The non-query path is what carries the
-- real affected-row count.
--
-- Unconditional apart from `is_active = 0`: no scope parameter, because purging is a whole-table maintenance
-- operation, and that matches the statement this replaces.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_purge_inactive;

CREATE PROCEDURE sp_config_images_locations_purge_inactive()
DELETE FROM config_images_locations
WHERE is_active = 0;
