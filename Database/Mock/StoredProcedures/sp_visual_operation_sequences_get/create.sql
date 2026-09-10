-- ============================================================
-- Stored Procedure: sp_visual_operation_sequences_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 2 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T021)
-- Shape key:  operation_sequences
-- ============================================================
--
-- Live projection: SequenceNumber, Description  (ORDER BY o.SEQUENCE_NO).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_operation_sequences_get;

CREATE PROCEDURE sp_visual_operation_sequences_get(
    IN p_normalized_work_order VARCHAR(64),
    IN p_part_number VARCHAR(64)
)
SELECT
    sequence_number AS SequenceNumber,
    description     AS Description
FROM visual_operation_sequences_result
WHERE normalized_work_order = p_normalized_work_order
  AND part_number = p_part_number
ORDER BY sequence_number;
