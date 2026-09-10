-- ============================================================
-- Stored Procedure: sp_visual_disposition_input_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 5 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T024)
-- Shape key:  disposition_input
-- ============================================================
--
-- Live projection: WorkOrderStatus, OpenWorkOrderQuantity, FinishedGoodsQuantity,
--                  HasOutsideVendorOperation   (the live read returns TOP (1)).
--
-- `WorkOrderStatus` is the raw Visual status code. Its MEANING is owned solely by
-- RequestDispositionStatusCodes (Open = {R,U,F}, Closed = {C}, X never FG); this read never
-- interprets it (FR-018).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_disposition_input_get;

CREATE PROCEDURE sp_visual_disposition_input_get(
    IN p_work_order VARCHAR(64),
    IN p_part_number VARCHAR(64)
)
SELECT
    work_order_status             AS WorkOrderStatus,
    open_work_order_quantity      AS OpenWorkOrderQuantity,
    finished_goods_quantity       AS FinishedGoodsQuantity,
    has_outside_vendor_operation  AS HasOutsideVendorOperation
FROM visual_disposition_input_result
WHERE work_order = p_work_order
  AND part_number = p_part_number
LIMIT 1;
