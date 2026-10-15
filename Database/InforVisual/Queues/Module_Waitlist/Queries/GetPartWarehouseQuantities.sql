-- ========================================
-- Module: Waitlist
-- Queue Script: Get Part Warehouse Quantities
-- Purpose: Return one part's on-hand quantity summed over ALL of its locations, and the same sum
--          restricted to finished-goods locations. Exactly one row per part, so the caller reads a
--          part-wide figure and never a per-location one.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Template: Module_Setup GetSubordinateParts.sql (PART / PART_LOCATION / WAREHOUSE joins)
-- Parameters:
--   @PartNumber - the part number, matched verbatim against PART.ID
--
-- Read shape `part_warehouse_quantities` (module ModuleWaitlist). The application reads this live
-- first and falls back to the mtm_mock mirror only when the reachability verdict is Cached or a live
-- attempt finds the source unreachable. The projection is copied verbatim into
-- part_warehouse_quantities_population.sql and into VisualReadShapeCatalog, so a cached answer and a
-- live answer cannot drift in shape.
--
-- WarehouseQuantity is the SUM of PART_LOCATION.QTY over every location the part is stocked in, not
-- PART.QTY_ON_HAND and not one row per location: PART carries a single plant-wide figure while the
-- caller needs the part-wide sum, however many locations it is spread over. A part that is known but
-- stocked nowhere returns one row of zeros rather than no row, and a part number the plant does not
-- know returns no row at all.
--
-- Finished-goods classification mirrors the taxonomy already documented for the WIP side in
-- Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql: a location is finished
-- goods when its own DESCRIPTION says so, when its WAREHOUSE.DESCRIPTION says so, or when its code is
-- 'FG' / 'DC-FG'. LOCATION is keyed by (WAREHOUSE_ID, ID), so the join carries both.
-- The codes are NOT YET CONFIRMED for this installation. Do not narrow them further and do not invent
-- a code: task T067's live validation
-- (Database/Validation/disposition_input_finished_goods/validate.sql) is what confirms them against
-- the plant system.
-- ========================================

SET NOCOUNT ON;

DECLARE @PartNumberNormalized nvarchar(60) = LTRIM(RTRIM(@PartNumber));

SELECT
    part.ID AS PartNumber,
    CAST(COALESCE(SUM(pl.QTY), 0) AS decimal(18, 4)) AS WarehouseQuantity,
    CAST(COALESCE(SUM(
        CASE
            WHEN loc.ID IN ('FG', 'DC-FG')
                OR loc.DESCRIPTION LIKE '%FINISHED GOODS%'
                OR wh.DESCRIPTION LIKE '%FINISHED GOODS%'
                THEN pl.QTY
            ELSE 0
        END
    ), 0) AS decimal(18, 4)) AS FinishedGoodsQuantity
FROM PART AS part
LEFT JOIN PART_LOCATION AS pl
    ON pl.PART_ID = part.ID
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = pl.WAREHOUSE_ID
LEFT JOIN LOCATION AS loc
    ON loc.WAREHOUSE_ID = pl.WAREHOUSE_ID
    AND loc.ID = pl.LOCATION_ID
WHERE
    part.ID = @PartNumberNormalized
GROUP BY
    part.ID;
