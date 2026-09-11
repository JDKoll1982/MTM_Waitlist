-- ============================================================
-- Stored Procedure: sp_visual_read_shape_freshness_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (freshness)
-- Created:    2026-09-10
-- Feature:    001-module-mock-visual-fallback (task T124)
-- ============================================================
--
-- Purpose
-- -------
-- Reports how current each shape's live mirror actually is, so the read-status surface and the
-- service status payload can state the cached-data age and whether a shape still holds only its
-- baseline seed rows (FR-017, FR-022, data-model.md §12).
--
-- Why this is a separate procedure
-- --------------------------------
-- `sp_visual_<shape>_get` may project ONLY the live read's own columns, or the structural identity
-- the fallback guarantees at FR-004 breaks; `refreshed_utc` therefore cannot be read through it.
-- `sp_visual_read_shape_metadata_get` answers "do the artifacts exist", not "how old is the data".
-- Neither can carry this information, so it is read here and nowhere else.
--
-- Contract
-- --------
--   IN  p_shape_key  VARCHAR(64)
--         NULL      => report every shape
--         Non-NULL  => report that shape only (no row when the shape is unknown)
--
--   OUT (one row per shape)
--     shape_key         the shape key
--     refreshed_utc     MAX(refreshed_utc) of the live mirror, NULL when the mirror is empty
--     is_seed_content   1 when the mirror holds no refreshed row (empty, or every row is seed
--                       content), 0 once a real refresh has landed
--     row_count         number of rows currently in the live mirror
--
-- Notes
-- -----
-- This is the one report that must enumerate the shapes: freshness is a per-table read, so it cannot
-- be derived from information_schema the way the metadata procedure is. Adding a sixth shape adds one
-- branch below. That is additive by construction — the output columns are fixed, so no existing
-- shape's contract, procedure signature, or result type changes (FR-016/FR-020).
--
-- An empty mirror reports is_seed_content = 1, because no refresh has ever succeeded for it, which is
-- exactly the state the baseline seed content exists to cover.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_read_shape_freshness_get;

CREATE PROCEDURE sp_visual_read_shape_freshness_get(
    IN p_shape_key VARCHAR(64)
)
SELECT
    f.shape_key,
    f.refreshed_utc,
    f.is_seed_content,
    f.row_count
FROM (
    SELECT
        'work_order_lookup' AS shape_key,
        MAX(refreshed_utc) AS refreshed_utc,
        CASE WHEN COUNT(*) = 0 OR SUM(is_seed_content) = COUNT(*) THEN 1 ELSE 0 END AS is_seed_content,
        COUNT(*) AS row_count
    FROM visual_work_order_lookup_result

    UNION ALL

    SELECT
        'operation_sequences' AS shape_key,
        MAX(refreshed_utc) AS refreshed_utc,
        CASE WHEN COUNT(*) = 0 OR SUM(is_seed_content) = COUNT(*) THEN 1 ELSE 0 END AS is_seed_content,
        COUNT(*) AS row_count
    FROM visual_operation_sequences_result

    UNION ALL

    SELECT
        'subordinate_parts' AS shape_key,
        MAX(refreshed_utc) AS refreshed_utc,
        CASE WHEN COUNT(*) = 0 OR SUM(is_seed_content) = COUNT(*) THEN 1 ELSE 0 END AS is_seed_content,
        COUNT(*) AS row_count
    FROM visual_subordinate_parts_result

    UNION ALL

    SELECT
        'inventory_locations' AS shape_key,
        MAX(refreshed_utc) AS refreshed_utc,
        CASE WHEN COUNT(*) = 0 OR SUM(is_seed_content) = COUNT(*) THEN 1 ELSE 0 END AS is_seed_content,
        COUNT(*) AS row_count
    FROM visual_inventory_locations_result

    UNION ALL

    SELECT
        'disposition_input' AS shape_key,
        MAX(refreshed_utc) AS refreshed_utc,
        CASE WHEN COUNT(*) = 0 OR SUM(is_seed_content) = COUNT(*) THEN 1 ELSE 0 END AS is_seed_content,
        COUNT(*) AS row_count
    FROM visual_disposition_input_result
) AS f
WHERE p_shape_key IS NULL OR f.shape_key = p_shape_key
ORDER BY f.shape_key;
