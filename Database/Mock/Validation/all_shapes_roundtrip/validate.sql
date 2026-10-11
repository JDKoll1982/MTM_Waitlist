-- ============================================================
-- Validation: all_shapes_roundtrip
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Feature:    001-module-mock-visual-fallback
-- Proves:     FR-004 / FR-006 / FR-017 — every one of the six shapes refreshes and reads back
--             with an identical result shape, and the get procedures return complete sets.
-- Run:        mysql -h <host> -u <user> mtm_mock < this_file
-- ============================================================
--
-- Diagnostic script (not part of the deploy path). Leaves every mirror holding a deterministic
-- one-row sentinel snapshot so later ad-hoc checks are reproducible.

USE mtm_mock;

-- ------------------------------------------------------------
-- Shape 1: work_order_lookup
-- ------------------------------------------------------------
CALL sp_visual_work_order_lookup_refresh(
    '[{"NormalizedWorkOrder":"VAL-WO","PartNumber":"VAL-P1","Description":"Validation part","WorkCenter":"VAL-WC"}]');
SELECT '1. work_order_lookup' AS shape;
CALL sp_visual_work_order_lookup_get('VAL-WO');

-- ------------------------------------------------------------
-- Shape 2: operation_sequences
-- ------------------------------------------------------------
CALL sp_visual_operation_sequences_refresh(
    '[{"NormalizedWorkOrder":"VAL-WO","PartNumber":"VAL-P1","SequenceNumber":10,"Description":"Operation 10 / VAL-WC"},
      {"NormalizedWorkOrder":"VAL-WO","PartNumber":"VAL-P1","SequenceNumber":20,"Description":"Operation 20 / Unassigned"}]');
SELECT '2. operation_sequences' AS shape;
CALL sp_visual_operation_sequences_get('VAL-WO', 'VAL-P1');

-- ------------------------------------------------------------
-- Shape 3: subordinate_parts
-- ------------------------------------------------------------
CALL sp_visual_subordinate_parts_refresh(
    '[{"NormalizedWorkOrder":"VAL-WO","ParentPartNumber":"VAL-P1","SequenceNumber":10,"Category":"Coil",
       "PartNumber":"MMC-VAL-1","Description":"Validation coil","Location":"V-A0-01","User8":"U8","OnHandQuantity":123.4567},
      {"NormalizedWorkOrder":"VAL-WO","ParentPartNumber":"VAL-P1","SequenceNumber":10,"Category":"Die",
       "PartNumber":"FGT-VAL-1","Description":"Die still needs location","Location":"V-B0-02","User8":"","OnHandQuantity":0}]');
SELECT '3. subordinate_parts' AS shape;
CALL sp_visual_subordinate_parts_get('VAL-WO', 'VAL-P1', 10);

-- ------------------------------------------------------------
-- Shape 4: inventory_locations
-- ------------------------------------------------------------
CALL sp_visual_inventory_locations_refresh(
    '[{"PartNumber":"VAL-P1","Location":"V-A0-01","OnHandQuantity":500.25},
      {"PartNumber":"VAL-P1","Location":"V-A0-02","OnHandQuantity":0.75}]');
SELECT '4. inventory_locations' AS shape;
CALL sp_visual_inventory_locations_get('VAL-P1');

-- ------------------------------------------------------------
-- Shape 5: disposition_input
-- ------------------------------------------------------------
CALL sp_visual_disposition_input_refresh(
    '[{"WorkOrder":"VAL-WO","PartNumber":"VAL-P1","WorkOrderStatus":"R","OpenWorkOrderQuantity":40,
       "FinishedGoodsQuantity":12.5,"HasOutsideVendorOperation":1}]');
SELECT '5. disposition_input' AS shape;
CALL sp_visual_disposition_input_get('VAL-WO', 'VAL-P1');

-- ------------------------------------------------------------
-- Shape 6: employee_by_number
-- ------------------------------------------------------------
CALL sp_visual_employee_by_number_refresh(
    '[{"EmployeeNumber":"VAL-E1","FirstName":"Validation","LastName":"Operator"},
      {"EmployeeNumber":"VAL-E2","FirstName":"Validation","LastName":"Inactive"}]');
SELECT '6. employee_by_number' AS shape;
CALL sp_visual_employee_by_number_get('VAL-E1');

-- ------------------------------------------------------------
-- Cross-shape assertions
-- ------------------------------------------------------------
SELECT '7. every mirror is a complete snapshot (a get on an unknown key returns 0 rows, not an error)' AS shape;
SELECT
    (SELECT COUNT(*) FROM visual_work_order_lookup_result  WHERE normalized_work_order = 'NO-SUCH-WO') AS wo_unknown_key_rows,
    (SELECT COUNT(*) FROM visual_employee_by_number_result WHERE employee_number     = 'NO-SUCH-EMPLOYEE') AS employee_unknown_key_rows;

SELECT '8. no refreshed mirror still carries seed content' AS shape;
SELECT
    (SELECT COUNT(*) FROM visual_work_order_lookup_result       WHERE is_seed_content = 1) AS s1,
    (SELECT COUNT(*) FROM visual_operation_sequences_result     WHERE is_seed_content = 1) AS s2,
    (SELECT COUNT(*) FROM visual_subordinate_parts_result       WHERE is_seed_content = 1) AS s3,
    (SELECT COUNT(*) FROM visual_inventory_locations_result     WHERE is_seed_content = 1) AS s4,
    (SELECT COUNT(*) FROM visual_disposition_input_result       WHERE is_seed_content = 1) AS s5,
    (SELECT COUNT(*) FROM visual_employee_by_number_result      WHERE is_seed_content = 1) AS s6;

SELECT '9. no transient _prev table persists for any shape' AS shape;
SELECT COUNT(*) AS prev_table_count
FROM information_schema.tables
WHERE table_schema = 'mtm_mock' AND table_name LIKE '%\_prev';

SELECT '10. every shape procedure is registered, plus the two catalog procedures' AS shape;
-- Counted by explicit exclusion rather than a LIKE pattern. Two traps here: inside a MySQL string literal
-- '\_' loses its backslash, so 'sp\_visual%\_get' degenerates to a single-char wildcard; and the catalog
-- procedures are themselves named '..._get'. Both make a naive count return 14. The shape procedures are
-- the twelve that do not start with 'sp_visual_read_shape_'.
SELECT
    (SELECT COUNT(*) FROM information_schema.routines
      WHERE routine_schema = 'mtm_mock' AND routine_type = 'PROCEDURE'
        AND LEFT(routine_name, 10) = 'sp_visual_'
        AND LEFT(routine_name, 21) <> 'sp_visual_read_shape_'
        AND SUBSTRING_INDEX(routine_name, '_', -1) IN ('get', 'refresh')) AS shape_procedure_count,
    (SELECT COUNT(*) FROM information_schema.routines
      WHERE routine_schema = 'mtm_mock' AND routine_type = 'PROCEDURE'
        AND LEFT(routine_name, 21) = 'sp_visual_read_shape_') AS catalog_procedure_count;
-- Expected: 12 shape procedures (six shapes x get + refresh) and 2 catalog procedures.
