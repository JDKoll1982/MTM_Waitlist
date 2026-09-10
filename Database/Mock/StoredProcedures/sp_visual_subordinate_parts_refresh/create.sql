-- ============================================================
-- Stored Procedure: sp_visual_subordinate_parts_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (refresh, shape 3 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T017)
-- Shape key:  subordinate_parts
-- ============================================================
--
-- Follows the T015 reference pattern exactly. See that file for the full rationale.
--
--   p_rows : JSON array of result objects. Keys:
--              "NormalizedWorkOrder", "ParentPartNumber", "SequenceNumber"  (the read's inputs)
--              "Category", "PartNumber", "Description", "Location", "User8",
--              "OnHandQuantity"                                             (the read's outputs)
--
-- KEY NAMING: the live read's input part number and its returned subordinate part number are both
-- called `PartNumber` in the Visual source. The payload therefore uses `ParentPartNumber` for the
-- INPUT (persisted as `parent_part_number`) so that `PartNumber` in a payload object means exactly
-- what it means in the live result (data-model.md §3.3).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_subordinate_parts_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_subordinate_parts_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    TRUNCATE TABLE visual_subordinate_parts_result_stage;

    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_subordinate_parts_result_stage (
        normalized_work_order,
        parent_part_number,
        sequence_number,
        category,
        part_number,
        description,
        location,
        user8,
        on_hand_quantity,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].NormalizedWorkOrder'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].ParentPartNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].SequenceNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Category'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].PartNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Description'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Location'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].User8'))),
        CAST(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].OnHandQuantity'))) AS DECIMAL(18,4)),
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

    SELECT COUNT(*) INTO v_loaded FROM visual_subordinate_parts_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_subordinate_parts_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    RENAME TABLE visual_subordinate_parts_result       TO visual_subordinate_parts_result_prev,
                 visual_subordinate_parts_result_stage TO visual_subordinate_parts_result,
                 visual_subordinate_parts_result_prev  TO visual_subordinate_parts_result_stage;
END $$

DELIMITER ;
