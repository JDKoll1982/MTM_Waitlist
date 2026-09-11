-- Stored Procedure: sp_config_images_locations_status_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: the `is_active` state of one (scope, scope_item_id) pair, whether or not the override is live.
--          Replaces the inline `SELECT is_active … LIMIT 1` in `ImageOverrideWriteService.CreateOverrideAsync`
--          (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   OUT zero or one row / one column: `is_active`. No row means the pair has never existed.
--
-- **There is deliberately no `is_active` filter here, and that is the whole point of the procedure.** The caller
-- has three distinct cases to tell apart before it can write — (a) no row, so INSERT; (b) an active row, so
-- report DUPLICATE_KEY; (c) an inactive row, which must be reactivated rather than re-inserted because the
-- unique key `uq_config_images_locations_scope_item` spans (scope, scope_item_id) regardless of `is_active`.
-- A filter here would collapse (b) and (c) into the same "no row" answer and turn case (c) into a duplicate-key
-- error at INSERT time. Every other read of this table filters to active rows; this one must not.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_status_get;

CREATE PROCEDURE sp_config_images_locations_status_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
)
SELECT is_active
FROM config_images_locations
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
LIMIT 1;
