-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Part warehouse quantities (shape 7 of 7: part_warehouse_quantities)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 7 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope: every part a caller can ask about by number — every PART row that holds at least one PART_LOCATION
--        row, plus every PART row whose part-level on-hand total is non-zero. This shape is keyed by the part
--        alone (`PartNumber` is both the input parameter and the returned part number), so unlike the
--        work-order-driven shapes there is no work-order key to enumerate from: the part IS the driver.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_part_warehouse_quantities_refresh):
--   PartNumber (input key AND returned part number), WarehouseQuantity, FinishedGoodsQuantity.
--
-- Both figures are PART-WIDE sum-of-locations figures, one row per part:
--   WarehouseQuantity      the part's on-hand quantity summed over ALL of its locations;
--   FinishedGoodsQuantity  that same sum restricted to its finished-goods locations.
--   Neither is a per-location figure and neither is read from PART.QTY_ON_HAND. FR-090 is the defect where a
--   part-level total stood in for the finished-goods figure, so both figures here are computed from
--   PART_LOCATION rows and nothing else. Consequence, stated so it is not later mistaken for a fault: a part
--   whose only stock is a part-level total with no PART_LOCATION rows still gets a row — it is worth answering
--   — and both of its figures are 0, which is exactly what its locations sum to.
--
-- Finished-goods location class: LOCATION.DESCRIPTION LIKE '%FINISHED GOODS%'
--   OR WAREHOUSE.DESCRIPTION LIKE '%FINISHED GOODS%' OR LOCATION.ID IN ('FG','DC-FG'). This mirrors the
--   taxonomy already documented for the WIP side in
--   Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql, where it was confirmed
--   2026-09-08 against live data (there: `md_locations.Location IN ('FG','DC-FG')` or name/building contains
--   'FINISHED GOODS'). The CODES ARE NOT YET CONFIRMED FOR THIS INSTALLATION on the Visual side: no Visual
--   LOCATION or WAREHOUSE row has been read for them from this repository, and no code is invented to fill the
--   gap. T067's live validation confirms them, and only then is this predicate authoritative.
--
-- The caller-side filters are deliberately NOT applied here (see the note in
--   Database/Mock/Tables/visual_inventory_locations_result/create.sql): the app's own on-hand >= 1 rule and its
--   ignored-location setting stay in the application, so the mirror holds every row the live read can return
--   and a part that drops to zero on hand is answered with 0 rather than disappearing.
-- ========================================

SET NOCOUNT ON;

SELECT
    part.ID AS PartNumber,
    CAST(COALESCE(SUM(COALESCE(pl.QTY, 0)), 0) AS decimal(18, 4)) AS WarehouseQuantity,
    CAST(
        COALESCE(SUM(
            CASE
                WHEN ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                    OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                    OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG')
                    THEN COALESCE(pl.QTY, 0)
                ELSE 0
            END
        ), 0)
        AS decimal(18, 4)
    ) AS FinishedGoodsQuantity
FROM PART AS part
LEFT JOIN PART_LOCATION AS pl
    ON pl.PART_ID = part.ID
LEFT JOIN LOCATION AS loc
    ON loc.ID = pl.LOCATION_ID
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = pl.WAREHOUSE_ID
WHERE
    -- Every part that holds stock anywhere: it has location rows, or a non-zero part-level total. A part with
    -- neither has nothing to answer and is left out, so the mirror stays bounded by real stock.
    EXISTS
    (
        SELECT 1
        FROM PART_LOCATION AS any_pl
        WHERE any_pl.PART_ID = part.ID
    )
    OR COALESCE(part.QTY_ON_HAND, 0) <> 0
GROUP BY
    part.ID
ORDER BY
    PartNumber;
