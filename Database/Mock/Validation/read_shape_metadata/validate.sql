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

SELECT '4. the derived key set matches the five expected shapes' AS step;
SELECT COUNT(*) AS shape_count
FROM (
    SELECT DISTINCT SUBSTRING(tb.table_name, 8, CHAR_LENGTH(tb.table_name) - 14) AS shape_key
      FROM information_schema.tables tb
     WHERE tb.table_schema = DATABASE()
       AND tb.table_name LIKE 'visual\_%\_result'
) AS k
WHERE k.shape_key IN ('work_order_lookup', 'operation_sequences', 'subordinate_parts',
                      'inventory_locations', 'disposition_input');
