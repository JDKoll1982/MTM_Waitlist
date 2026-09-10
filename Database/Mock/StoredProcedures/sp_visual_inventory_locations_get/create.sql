-- ============================================================
-- Stored Procedure: sp_visual_inventory_locations_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 4 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T023)
-- Shape key:  inventory_locations
-- ============================================================
--
-- Live projection: PartNumber, Location, OnHandQuantity  (ORDER BY Location, part.ID).
--
-- The caller-side on-hand >= 1 filter and ignored-location filtering stay in the caller and are
-- deliberately NOT applied here (data-model.md §3.4) — the mirror returns the same rows the live
-- read would, and the caller filters identically whichever source answered.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_inventory_locations_get;

CREATE PROCEDURE sp_visual_inventory_locations_get(
    IN p_part_number VARCHAR(64)
)
SELECT
    part_number       AS PartNumber,
    location          AS Location,
    on_hand_quantity  AS OnHandQuantity
FROM visual_inventory_locations_result
WHERE part_number = p_part_number
ORDER BY location, part_number;
