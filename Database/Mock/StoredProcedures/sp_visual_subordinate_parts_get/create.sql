-- ============================================================
-- Stored Procedure: sp_visual_subordinate_parts_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 3 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T022)
-- Shape key:  subordinate_parts
-- ============================================================
--
-- Live projection: Category, PartNumber, Description, Location, User8, OnHandQuantity
-- (ORDER BY Category, req.PART_ID).
--
-- The live read takes the OPERATION's part number as an input. That input is persisted as
-- `parent_part_number`, and the procedure exposes it as `p_part_number` so callers see the same
-- parameter name and meaning as the live read (data-model.md §3.3). `PartNumber` in the RETURNED
-- rows is always the subordinate part.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_subordinate_parts_get;

CREATE PROCEDURE sp_visual_subordinate_parts_get(
    IN p_normalized_work_order VARCHAR(64),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number INT
)
SELECT
    category          AS Category,
    part_number       AS PartNumber,
    description       AS Description,
    location          AS Location,
    user8             AS User8,
    on_hand_quantity  AS OnHandQuantity
FROM visual_subordinate_parts_result
WHERE normalized_work_order = p_normalized_work_order
  AND parent_part_number = p_part_number
  AND sequence_number = p_sequence_number
ORDER BY category, part_number;
