-- ============================================================
-- Stored Procedure: sp_visual_part_warehouse_quantities_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 7 of 7)
-- Created:    2026-10-10
-- Feature:    012-waitlist-card-layout (task T020)
-- Shape key:  part_warehouse_quantities
-- ============================================================
--
-- Live projection: PartNumber, WarehouseQuantity, FinishedGoodsQuantity  (one row per part).
--
-- `part_number` is both the read's input parameter and its returned identifying column, exactly as
-- shape 4 (`inventory_locations`) arranges it, so the procedure takes `p_part_number` and the caller
-- sees the same name and meaning as the live read (FR-090).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_part_warehouse_quantities_get;

CREATE PROCEDURE sp_visual_part_warehouse_quantities_get(
    IN p_part_number VARCHAR(64)
)
SELECT
    part_number                AS PartNumber,
    warehouse_quantity         AS WarehouseQuantity,
    finished_goods_quantity    AS FinishedGoodsQuantity
FROM visual_part_warehouse_quantities_result
WHERE part_number = p_part_number;
