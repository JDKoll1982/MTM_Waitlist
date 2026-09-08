-- ========================================
-- Module: Waitlist
-- MySQL Queue Script: Get WIP Floor Quantities
-- Purpose: Return the MTM WIP Application floor snapshot for a part, bucketing live inv_inventory
--          rows by location disposition (Finished Goods / Outside Service / Non-Conforming / WIP).
--          Feeds MTM_Waitlist.Settings RequestDispositionClassifier via the disposition resolver as a
--          live floor-level cross-check next to the Infor Visual WORK_ORDER/OPERATION snapshot.
-- Target: MTM WIP Application MySQL (mtm_wip_application_winforms)
-- Tables: inv_inventory (PartID, Location, Quantity, ItemType) + md_locations (Location, Building)
-- Parameters:
--   @PartNumber
--
-- Disposition-by-location (confirmed 2026-09-08 against live data):
--   Finished Goods    md_locations.Location IN ('FG','DC-FG') or name/building contains 'FINISHED GOODS'
--   Outside Service   Location LIKE 'O/S%' (e.g. 'O/S - VITS' staging)
--   Non-Conforming    Location LIKE 'NCM%' (NCM / NCM - VITS / NCM-VITS / NCM/NA)
--   WIP               every remaining floor/rack location (R-*, U-*, FLOOR-*, ...)
-- Dunnage rows (ItemType='Dunnage') are excluded - they are packaging stock, not product.
-- Returns exactly one row; when the part has no floor stock every bucket is 0.
-- ========================================

SELECT
    COALESCE(SUM(
        CASE WHEN bucket.IsFinishedGoods = 1 THEN bucket.Quantity ELSE 0 END
    ), 0) AS FinishedGoodsFloorQuantity,
    COALESCE(SUM(
        CASE WHEN bucket.IsOutsideService = 1 THEN bucket.Quantity ELSE 0 END
    ), 0) AS OutsideServiceFloorQuantity,
    COALESCE(SUM(
        CASE WHEN bucket.IsNonConforming = 1 THEN bucket.Quantity ELSE 0 END
    ), 0) AS NonConformingFloorQuantity,
    COALESCE(SUM(
        CASE WHEN bucket.IsFinishedGoods = 0 AND bucket.IsOutsideService = 0 AND bucket.IsNonConforming = 0
             THEN bucket.Quantity ELSE 0 END
    ), 0) AS WipFloorQuantity
FROM
(
    SELECT
        inv.Quantity,
        CASE WHEN loc.Location IS NOT NULL
                  AND (loc.Location IN ('FG', 'DC-FG')
                       OR loc.Location LIKE '%FINISHED GOODS%'
                       OR loc.Building LIKE '%FINISHED GOODS%')
             THEN 1 ELSE 0 END AS IsFinishedGoods,
        CASE WHEN loc.Location IS NOT NULL
                  AND (loc.Location LIKE 'O/S%' OR loc.Building LIKE 'O/S%')
             THEN 1 ELSE 0 END AS IsOutsideService,
        CASE WHEN loc.Location IS NOT NULL AND loc.Location LIKE 'NCM%'
             THEN 1 ELSE 0 END AS IsNonConforming
    FROM inv_inventory AS inv
    LEFT JOIN md_locations AS loc
        ON loc.Location = inv.Location
    WHERE inv.PartID = LTRIM(RTRIM(@PartNumber))
      AND inv.ItemType <> 'Dunnage'
) AS bucket;
