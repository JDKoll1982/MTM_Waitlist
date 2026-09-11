-- Stored Procedure: sp_config_images_locations_count_active_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: how many active image overrides exist in total. Replaces the inline `SELECT COUNT(*)` in
--          `ImageOverrideReadService.CountAllActiveOverridesAsync` (FR-015).
--
-- Contract:
--   OUT one row / one column: `count`. The caller reads it by column name, so the alias is part of the contract.
--
-- Unfiltered apart from `is_active = 1`, exactly like the statement it replaces: the count drives the Settings
-- summary line ("N images overridden"), which counts live overrides only.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_count_active_get;

CREATE PROCEDURE sp_config_images_locations_count_active_get()
SELECT COUNT(*) as count
FROM config_images_locations
WHERE is_active = 1;
