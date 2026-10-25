-- ============================================================
-- Validation: read_shape_metadata
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Feature:    001-module-mock-visual-fallback (task T109)
-- Proves:     sp_visual_read_shape_metadata_get reports artifact existence and the mirror's
--             actual columns, and still reports a shape whose artifacts are missing (so a
--             half-added shape is visible rather than silently absent — FR-016/FR-020).
-- Run:        mysql -h <host> -u <user> mtm_mock < this_file
-- ============================================================

USE mtm_mock;

SELECT '1. every deployed shape is reported once with all four artifacts present' AS step;
CALL sp_visual_read_shape_metadata_get(NULL);

SELECT '2. a single requested shape reports its actual physical columns' AS step;
CALL sp_visual_read_shape_metadata_get('work_order_lookup');

SELECT '3. a requested shape with no artifacts is still reported (half-added detection)' AS step;
CALL sp_visual_read_shape_metadata_get('definitely_not_a_shape');

SELECT '4. the derived key set matches the eight expected shapes' AS step;
SELECT COUNT(*) AS shape_count
FROM (
    SELECT DISTINCT SUBSTRING(tb.table_name, 8, CHAR_LENGTH(tb.table_name) - 14) AS shape_key
      FROM information_schema.tables tb
     WHERE tb.table_schema = DATABASE()
       AND tb.table_name LIKE 'visual\_%\_result'
) AS k
WHERE k.shape_key IN ('work_order_lookup', 'operation_sequences', 'subordinate_parts',
                      'inventory_locations', 'disposition_input', 'employee_by_number',
                      'part_warehouse_quantities', 'work_order_search');

-- Three shapes are asserted by name as well as counted, because a count of eight is satisfied by any eight: these
-- ask whether the two shapes the later features added are present and complete (011's FR-032/FR-033, 012's
-- FR-090). A shape whose artifacts are missing is reported rather than silently absent, which is the same rule
-- step 3 applies to an unknown shape.
SELECT '5. the sixth shape reports all four artifacts present' AS step;
CALL sp_visual_read_shape_metadata_get('employee_by_number');

SELECT '6. the seventh shape reports all four artifacts present' AS step;
CALL sp_visual_read_shape_metadata_get('part_warehouse_quantities');

SELECT '6b. the eighth shape (the Setup work-order search) reports all four artifacts present' AS step;
CALL sp_visual_read_shape_metadata_get('work_order_search');

-- ------------------------------------------------------------
-- The freshness report is a separate artifact that must enumerate the shapes by hand, so it is checked here too.
-- Read-only: it only selects from the mirrors. Every one of the eight shapes must appear, because a shape the
-- report omits is reported by the service status payload and by the app's read-status indicator as
-- refreshedUtc null / isSeedContentOnly true, however successfully that shape refreshes.
-- ------------------------------------------------------------
SELECT '7. the freshness report names every one of the eight shapes (expect eight rows, one per shape)' AS step;
CALL sp_visual_read_shape_freshness_get(NULL);

SELECT '8. a single requested shape reports its own freshness row' AS step;
CALL sp_visual_read_shape_freshness_get('part_warehouse_quantities');
