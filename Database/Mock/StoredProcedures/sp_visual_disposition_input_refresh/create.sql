-- ============================================================
-- Stored Procedure: sp_visual_disposition_input_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (refresh, shape 5 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T019)
-- Shape key:  disposition_input
-- ============================================================
--
-- Follows the T015 reference pattern exactly. See that file for the full rationale.
--
--   p_rows : JSON array of result objects. Keys:
--              "WorkOrder", "PartNumber"      (the read's inputs)
--              "WorkOrderStatus", "OpenWorkOrderQuantity", "FinishedGoodsQuantity",
--              "HasOutsideVendorOperation"     (the read's outputs)
--
-- `WorkOrderStatus` is stored as the raw Visual status code. The status MEANING remains owned
-- solely by RequestDispositionStatusCodes; this cache never interprets it (FR-018).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_disposition_input_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_disposition_input_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    TRUNCATE TABLE visual_disposition_input_result_stage;

    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_disposition_input_result_stage (
        work_order,
        part_number,
        work_order_status,
        open_work_order_quantity,
        finished_goods_quantity,
        has_outside_vendor_operation,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].WorkOrder'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].PartNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].WorkOrderStatus'))),
        CAST(IFNULL(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].OpenWorkOrderQuantity'))), '0') AS DECIMAL(18,4)),
        CAST(IFNULL(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].FinishedGoodsQuantity'))), '0') AS DECIMAL(18,4)),
        CAST(IFNULL(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].HasOutsideVendorOperation'))), '0') AS SIGNED),
        UTC_TIMESTAMP(),
        0
    FROM (
        SELECT (ones.n + tens.n * 10 + hundreds.n * 100 + thousands.n * 1000) AS i
        FROM (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS ones
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS tens
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS hundreds
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1) AS thousands
    ) AS n
    WHERE n.i < v_expected;

    SELECT COUNT(*) INTO v_loaded FROM visual_disposition_input_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_disposition_input_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    RENAME TABLE visual_disposition_input_result       TO visual_disposition_input_result_prev,
                 visual_disposition_input_result_stage TO visual_disposition_input_result,
                 visual_disposition_input_result_prev  TO visual_disposition_input_result_stage;
END $$

DELIMITER ;
