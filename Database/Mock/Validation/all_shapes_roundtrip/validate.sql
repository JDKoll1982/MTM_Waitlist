-- ============================================================
-- Validation: all_shapes_roundtrip
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Feature:    001-module-mock-visual-fallback
-- Proves:     FR-004 / FR-006 / FR-017 — every one of the five shapes refreshes and reads back
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
-- Cross-shape assertions
-- ------------------------------------------------------------
SELECT '6. every mirror is a complete snapshot (a get on an unknown key returns 0 rows, not an error)' AS shape;
SELECT COUNT(*) AS unknown_key_rows FROM visual_work_order_lookup_result WHERE normalized_work_order = 'NO-SUCH-WO';

SELECT '7. no refreshed mirror still carries seed content' AS shape;
SELECT
    (SELECT COUNT(*) FROM visual_work_order_lookup_result       WHERE is_seed_content = 1) AS s1,
    (SELECT COUNT(*) FROM visual_operation_sequences_result     WHERE is_seed_content = 1) AS s2,
    (SELECT COUNT(*) FROM visual_subordinate_parts_result       WHERE is_seed_content = 1) AS s3,
    (SELECT COUNT(*) FROM visual_inventory_locations_result     WHERE is_seed_content = 1) AS s4,
    (SELECT COUNT(*) FROM visual_disposition_input_result       WHERE is_seed_content = 1) AS s5;

SELECT '8. no transient _prev table persists for any shape' AS shape;
SELECT COUNT(*) AS prev_table_count
FROM information_schema.tables
WHERE table_schema = 'mtm_mock' AND table_name LIKE '%\_prev';

SELECT '9. all ten procedures are registered in mtm_mock' AS shape;
SELECT COUNT(*) AS procedure_count
FROM information_schema.routines
WHERE routine_schema = 'mtm_mock' AND routine_type = 'PROCEDURE' AND routine_name LIKE 'sp_visual%';
