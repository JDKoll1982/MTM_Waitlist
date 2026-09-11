-- Stored Procedure: sp_config_images_locations_recent_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: the most recently updated overrides, capped by the caller. Replaces the inline SELECT in
--          `ImageOverrideReadService.GetRecentlyUpdatedOverridesAsync` (FR-015).
--
-- Contract:
--   IN  p_max_rows  INT     how many rows to return
--   OUT up to `p_max_rows` rows (the same ten columns), ordered `updated_utc DESC`.
--
-- **This is the site whose `LIMIT` used to be interpolated straight into the statement text**
-- (`LIMIT {maxRecordCount}`), so it is the one read that genuinely needed a parameter. MySQL accepts an
-- integer-valued routine parameter directly in `LIMIT`, so the cap binds as `p_max_rows`.
--
-- Verified live on 5.7.24, because the accepted forms are narrower than they look: a routine parameter in `LIMIT`
-- works, and any expression does not — `LIMIT GREATEST(p,1)` is rejected with `ERROR 1327 Undeclared variable:
-- GREATEST`. A `BEGIN … END` body would be needed to clamp into a local variable first, and no other artifact in
-- this repo uses one (the client splits a routine body on `;` without a `DELIMITER` directive), so the clamp is
-- not applied here. `LIMIT 0` is legal and returns no rows; only `LIMIT NULL` errors, which the caller's
-- `maxRecordCount < 1` guard (`ArgumentException`) already prevents.
--
-- No `is_active` filter, deliberately, matching the statement this replaces: "recently updated" is an audit view
-- over the table and must be able to show a withdrawn override — that is how an operator sees what was
-- deactivated. Adding the filter here would silently hide those rows.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_recent_get;

CREATE PROCEDURE sp_config_images_locations_recent_get(
    IN p_max_rows INT
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
ORDER BY updated_utc DESC
LIMIT p_max_rows;
