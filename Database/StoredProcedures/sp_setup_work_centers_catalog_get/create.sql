-- Stored Procedure: sp_setup_work_centers_catalog_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T090)
--
-- Purpose: the active work-center catalog with its display rank, in the order the Settings screen shows it.
--          Replaces the inline SELECT in `ImageLocationService.LoadWorkCenterDetailsAsync` (FR-015).
--
-- Why this exists instead of routing that call site to sp_setup_work_centers_get_all
-- -----------------------------------------------------------------------------------
-- T090 asked for exactly that routing and required the `IN (...)` filter semantics to be verified first. They do
-- not match, in three ways, so the switch was not made:
--
--   * that procedure does **not** select `sort_rank`, and this caller maps it into `WorkCenterItem.SortRank` —
--     every rank would have silently become 0;
--   * it orders by `sort_rank, work_center_name`, while this caller orders by `building, sort_rank,
--     work_center_name` — all 25 rows would have been reordered;
--   * it takes no parameters, so it cannot express `work_center_name IN (…)` at all.
--
-- Making it serve this caller would have meant changing a procedure that other call sites (the work-center
-- catalog and the image-override dialog) already depend on. A separate read is additive and leaves them alone.
--
-- The name filter is applied by the caller, not here: a variable-length name list has no parameter form in
-- MySQL 5.7 that is not a fragile delimited string, and the active catalog is small and bounded (25 rows live on
-- 2026-09-10), so narrowing it in C# costs nothing measurable and cannot break on a name containing a comma.
-- Everything else — the `is_active = 1` scope and the ordering — is preserved exactly.
--
-- Contract:
--   OUT one row per active work center: id, work_center_name, building, sort_rank, is_active
--       ordered by building ASC, sort_rank ASC, work_center_name ASC.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_catalog_get;

CREATE PROCEDURE sp_setup_work_centers_catalog_get()
SELECT
    id,
    work_center_name,
    building,
    sort_rank,
    is_active
FROM vw_setup_work_centers_active
ORDER BY building ASC, sort_rank ASC, work_center_name ASC;
