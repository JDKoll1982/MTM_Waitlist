-- ============================================================
-- Stored Procedure: sp_visual_work_order_lookup_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 1 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T020)
-- Shape key:  work_order_lookup
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual
-- read returns (FR-004), so a caller cannot distinguish cached from live:
--   live : PartNumber, Description, WorkCenter   (ORDER BY wo.PART_ID)
--   here : same names/order, projected from the mirror's own columns.
--
-- Before the first successful refresh the mirror holds baseline seed rows (is_seed_content = 1),
-- so a fresh install returns a usable result rather than an error (FR-017).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_work_order_lookup_get;

CREATE PROCEDURE sp_visual_work_order_lookup_get(
    IN p_normalized_work_order VARCHAR(64)
)
SELECT
    part_number  AS PartNumber,
    description  AS Description,
    work_center  AS WorkCenter
FROM visual_work_order_lookup_result
WHERE normalized_work_order = p_normalized_work_order
ORDER BY part_number;
