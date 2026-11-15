-- ============================================================
-- Stored Procedure: sp_visual_part_sequence_state_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 11 of 12)
-- Created:    2026-11-15
-- Feature:    001-module-mock-visual-fallback (read shape 11, playbook step 3)
-- Shape key:  part_sequence_state
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual read
-- returns, so a caller cannot distinguish cached from live (FR-004):
--   live : OperationStatus, IsLastOperation, NextResource
--   here : same names and order, at most one row.
--
-- The sequence is compared as text, exactly as it is stored. The live read converts a non-numeric
-- sequence to "no operation", so a value that names no sequence here returns no row, which is the same
-- answer.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_part_sequence_state_get;

DELIMITER $$

CREATE PROCEDURE sp_visual_part_sequence_state_get(
    IN p_part_number VARCHAR(60),
    IN p_sequence VARCHAR(20)
)
BEGIN
    DECLARE v_part_number VARCHAR(60);
    DECLARE v_sequence VARCHAR(20);

    SET v_part_number = TRIM(COALESCE(p_part_number, ''));
    SET v_sequence = TRIM(COALESCE(p_sequence, ''));

    SELECT
        COALESCE(state.operation_status, '') AS OperationStatus,
        state.is_last_operation AS IsLastOperation,
        COALESCE(state.next_resource, '') AS NextResource
    FROM visual_part_sequence_state_result AS state
    WHERE
        v_part_number <> ''
        AND v_sequence <> ''
        AND state.part_number = v_part_number
        AND state.sequence = v_sequence
    LIMIT 1;
END $$

DELIMITER ;