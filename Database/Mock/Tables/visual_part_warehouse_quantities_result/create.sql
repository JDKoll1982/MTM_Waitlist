-- ============================================================
-- Object:      visual_part_warehouse_quantities_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 7 of 7)
-- Created:     2026-10-10
-- Feature:     012-waitlist-card-layout (task T018)
-- Shape key:   part_warehouse_quantities
-- Source read: Database/InforVisual/Queues/Module_Waitlist/Queries/GetPartWarehouseQuantities.sql
-- Caller:      the card's `Quantity in house`, `Quantity` and `Total Qty In House` cells (FR-090)
-- ============================================================
--
-- See visual_work_order_lookup_result/create.sql for the shared mirror-table rationale.
--
-- One row per part, so `part_number` is BOTH the read's input parameter and its identifying output
-- column — the same arrangement shape 4 (`inventory_locations`) uses. Both figures are location sums:
-- `warehouse_quantity` is the part-wide total and `finished_goods_quantity` is the same sum restricted
-- to finished-goods locations. Neither is `PART.QTY_ON_HAND`, which is the FR-090 defect this shape
-- exists to replace.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_part_warehouse_quantities_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    part_number VARCHAR(64) NOT NULL,
    warehouse_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_part_warehouse_quantities_result_part (part_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
