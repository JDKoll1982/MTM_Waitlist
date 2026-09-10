-- ============================================================
-- Stored Procedure: sp_visual_read_shape_metadata_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (metadata)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T109)
-- ============================================================
--
-- Purpose
-- -------
-- Reports, for a requested read shape (or for every shape present), whether that shape's
-- artifacts actually exist in `mtm_mock`, plus the mirror table's ACTUAL column list taken
-- from information_schema.
--
-- This exists so the service's shape-catalog startup validation (RefreshShapeCatalogProvider,
-- tasks T029/T086/T110) can check catalog-to-artifact agreement WITHOUT hand-writing schema
-- queries in C#. Constitution III forbids inline/hard-coded SQL statement text in application
-- code, and task T098's inline-SQL audit would otherwise need an exemption for metadata reads.
-- Routing the check through this procedure keeps the SP-first rule intact with no exemption.
--
-- Contract (data-model.md §12)
-- ---------------------------
--   IN  p_shape_key  VARCHAR(64)
--         A shape key such as 'work_order_lookup'.
--         NULL => report every shape whose mirror table exists.
--         Non-NULL => always report that shape, even when none of its artifacts exist
--                     (so a half-added shape is reported rather than silently absent).
--
--   OUT (one row per shape)
--     shape_key                 the requested/derived shape key
--     mirror_table_exists       1 when visual_<key>_result exists
--     stage_table_exists        1 when visual_<key>_result_stage exists
--     get_procedure_exists      1 when sp_visual_<key>_get exists
--     refresh_procedure_exists  1 when sp_visual_<key>_refresh exists
--     mirror_columns            comma-joined actual column names (NULL when the table is absent)
--
-- Notes
-- -----
-- `mirror_columns` lists PHYSICAL columns, so it includes the metadata columns `id`,
-- `refreshed_utc`, and `is_seed_content`. The catalog provider compares only the projection
-- columns when validating `outputColumns` (see RefreshShapeCatalogProvider).
--
-- The derived key is computed as SUBSTRING(table_name, 8, LENGTH - 14): `visual_` is 7
-- characters and `_result` is 7, so the middle is the shape key verbatim.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_read_shape_metadata_get;

CREATE PROCEDURE sp_visual_read_shape_metadata_get(
    IN p_shape_key VARCHAR(64)
)
SELECT
    k.shape_key,
    (SELECT COUNT(*)
       FROM information_schema.tables tb
      WHERE tb.table_schema = DATABASE()
        AND tb.table_name = CONCAT('visual_', k.shape_key, '_result')) AS mirror_table_exists,
    (SELECT COUNT(*)
       FROM information_schema.tables tb
      WHERE tb.table_schema = DATABASE()
        AND tb.table_name = CONCAT('visual_', k.shape_key, '_result_stage')) AS stage_table_exists,
    (SELECT COUNT(*)
       FROM information_schema.routines r
      WHERE r.routine_schema = DATABASE()
        AND r.routine_name = CONCAT('sp_visual_', k.shape_key, '_get')) AS get_procedure_exists,
    (SELECT COUNT(*)
       FROM information_schema.routines r
      WHERE r.routine_schema = DATABASE()
        AND r.routine_name = CONCAT('sp_visual_', k.shape_key, '_refresh')) AS refresh_procedure_exists,
    (SELECT GROUP_CONCAT(c.column_name ORDER BY c.ordinal_position)
       FROM information_schema.columns c
      WHERE c.table_schema = DATABASE()
        AND c.table_name = CONCAT('visual_', k.shape_key, '_result')) AS mirror_columns
FROM (
    SELECT DISTINCT SUBSTRING(tb.table_name, 8, CHAR_LENGTH(tb.table_name) - 14) AS shape_key
      FROM information_schema.tables tb
     WHERE tb.table_schema = DATABASE()
       AND tb.table_name LIKE 'visual\_%\_result'
    UNION
    SELECT p_shape_key FROM DUAL
) AS k
WHERE k.shape_key IS NOT NULL
  AND (p_shape_key IS NULL OR k.shape_key = p_shape_key)
ORDER BY k.shape_key;
