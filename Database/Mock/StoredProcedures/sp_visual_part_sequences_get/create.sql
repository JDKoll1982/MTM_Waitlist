-- ============================================================
-- Stored Procedure: sp_visual_part_sequences_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 10 of 12)
-- Created:    2026-11-15
-- Feature:    001-module-mock-visual-fallback (read shape 10, playbook step 3)
-- Shape key:  part_sequences
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual read
-- returns, so a caller cannot distinguish cached from live (FR-004):
--   live : Sequence, Description, OperationStatus, IsLastOperation, NextResource, CancelledOnly,
--          SequenceSource
--   here : same names and order, with the same ordering rule.
--
-- The ordering is copied from GetPartSequences.sql: sequence number first, then the relation, because
-- OwnPart sorts before Requirement and the caller takes the first row it sees for a number. A sequence
-- is numeric in the plant's data, but the column is text, so the guard keeps a non-numeric value from
-- sorting ahead of every real sequence.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_part_sequences_get;

DELIMITER $$

CREATE PROCEDURE sp_visual_part_sequences_get(
    IN p_part_number VARCHAR(60)
)
BEGIN
    DECLARE v_part_number VARCHAR(60);

    SET v_part_number = TRIM(COALESCE(p_part_number, ''));

    SELECT
        sequences.sequence AS Sequence,
        COALESCE(sequences.description, '') AS Description,
        COALESCE(sequences.operation_status, '') AS OperationStatus,
        sequences.is_last_operation AS IsLastOperation,
        COALESCE(sequences.next_resource, '') AS NextResource,
        sequences.cancelled_only AS CancelledOnly,
        sequences.sequence_source AS SequenceSource
    FROM visual_part_sequences_result AS sequences
    WHERE v_part_number <> '' AND sequences.part_number = v_part_number
    ORDER BY
        CASE
            WHEN sequences.sequence REGEXP '^[0-9]+$' THEN CAST(sequences.sequence AS UNSIGNED)
            ELSE 4294967295
        END,
        sequences.sequence,
        sequences.sequence_source;
END $$

DELIMITER ;
