-- ============================================================
-- Stored Procedure: sp_visual_operation_sequences_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (refresh, shape 2 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T016)
-- Shape key:  operation_sequences
-- ============================================================
--
-- Follows the T015 reference pattern exactly (see sp_visual_work_order_lookup_refresh/create.sql
-- for the full rationale): truncate stage → load complete payload → validate → atomic 3-name swap.
--
--   p_rows : JSON array of result objects. Keys:
--              "NormalizedWorkOrder", "PartNumber"      (the read's input keys)
--              "SequenceNumber", "Description"          (the read's outputs)
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_operation_sequences_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_operation_sequences_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    TRUNCATE TABLE visual_operation_sequences_result_stage;

    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_operation_sequences_result_stage (
        normalized_work_order,
        part_number,
        sequence_number,
        description,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].NormalizedWorkOrder'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].PartNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].SequenceNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Description'))),
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

    SELECT COUNT(*) INTO v_loaded FROM visual_operation_sequences_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_operation_sequences_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    RENAME TABLE visual_operation_sequences_result       TO visual_operation_sequences_result_prev,
                 visual_operation_sequences_result_stage TO visual_operation_sequences_result,
                 visual_operation_sequences_result_prev  TO visual_operation_sequences_result_stage;
END $$

DELIMITER ;
