-- ============================================================
-- Stored Procedure: sp_visual_part_warehouse_quantities_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (refresh, shape 7 of 7)
-- Created:    2026-10-10
-- Feature:    012-waitlist-card-layout (task T021)
-- Shape key:  part_warehouse_quantities
-- ============================================================
--
-- Follows the T015 reference pattern exactly. See that file for the full rationale.
--
--   p_rows : JSON array of result objects. Keys:
--              "PartNumber"          (the read's input AND its returned part number)
--              "WarehouseQuantity", "FinishedGoodsQuantity"   (the read's outputs)
--
-- The swap is one statement, so a refresh that fails its count check leaves the live snapshot
-- untouched, and the transient `_prev` name never persists between cycles.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_part_warehouse_quantities_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_part_warehouse_quantities_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    TRUNCATE TABLE visual_part_warehouse_quantities_result_stage;

    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_part_warehouse_quantities_result_stage (
        part_number,
        warehouse_quantity,
        finished_goods_quantity,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].PartNumber'))),
        CAST(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].WarehouseQuantity'))) AS DECIMAL(18,4)),
        CAST(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].FinishedGoodsQuantity'))) AS DECIMAL(18,4)),
        UTC_TIMESTAMP(),
        0
    FROM (
        SELECT (ones.n + tens.n * 10 + hundreds.n * 100 + thousands.n * 1000 + ten_thousands.n * 10000) AS i
        FROM (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS ones
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS tens
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS hundreds
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS thousands
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS ten_thousands
    ) AS n
    WHERE n.i < v_expected;

    SELECT COUNT(*) INTO v_loaded FROM visual_part_warehouse_quantities_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_part_warehouse_quantities_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    RENAME TABLE visual_part_warehouse_quantities_result       TO visual_part_warehouse_quantities_result_prev,
                 visual_part_warehouse_quantities_result_stage TO visual_part_warehouse_quantities_result,
                 visual_part_warehouse_quantities_result_prev  TO visual_part_warehouse_quantities_result_stage;
END $$

DELIMITER ;
