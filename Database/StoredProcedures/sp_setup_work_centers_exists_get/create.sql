-- Stored Procedure: sp_setup_work_centers_exists_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: does this work center exist, so the orphan detector can tell a live override from an orphaned one.
--          Replaces the inline `SELECT id FROM setup_work_centers_catalog WHERE id = @p_id LIMIT 1;` in
--          `ImageOverrideReadService.ValidateWorkCenterExistsAsync` (FR-015).
--
-- Contract:
--   IN  p_id  BIGINT
--   OUT zero or one row / one column: the id, when it exists. The caller only asks whether it got a row back.
--
-- Note the deliberate difference from `sp_setup_work_centers_get_all` and
-- `sp_setup_work_centers_catalog_get`: **no `is_active` filter.** This answers "does this work center exist",
-- which the orphan detector needs in order to distinguish "the work center was deleted from the catalog" from
-- "the work center is merely inactive right now" — filtering to active rows would report every override on a
-- temporarily inactive work center as an orphan and offer the operator a cleanup that should not happen.
-- That matches the statement this replaces, which also had no `is_active` predicate.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_exists_get;

CREATE PROCEDURE sp_setup_work_centers_exists_get(
    IN p_id BIGINT
)
SELECT id
FROM setup_work_centers_catalog
WHERE id = p_id
LIMIT 1;
