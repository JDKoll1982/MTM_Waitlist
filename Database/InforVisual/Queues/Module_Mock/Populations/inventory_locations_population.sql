-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Inventory locations (shape 4 of 5: inventory_locations)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 4 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope (operator decision, 2026-09-10): the parts of the ADDRESSABLE open work-order population —
--        every part that is on an addressable open order (WORK_ORDER.PART_ID) plus every part that
--        order requires (REQUIREMENT.PART_ID). "All parts" is deliberately NOT cached: the PART table
--        is not bounded by work-order activity and would put unrelated master data in the cache.
--        Addressability is the `WO-######` form the live reads resolve (operator decision 2026-09-12,
--        task T144); see work_order_lookup_population.sql for the full rationale.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_inventory_locations_refresh):
--   PartNumber (input key AND returned part number), Location, OnHandQuantity.
--
-- One row per (part, location): the mirror carries
--   uq_visual_inventory_locations_result_location (part_number, location), and Infor Visual reports
--   inventory as ONE pooled row per part-in-location, so the per-location value is the maximum of the
--   rows GetInventoryLocations.sql would return for that part.
--
-- Zero-stock locations are NOT cached (operator decision 2026-09-12). A pooled row whose quantity is
--   below 1 is invisible to the caller anyway - the Waitlist detail grid keeps only on-hand >= 1
--   (InventoryLocationFiltering) - so caching it only bloats the mirror. Each (part, location) pair
--   holds at most one row and the whole table is replaced on every refresh
--   (sp_visual_inventory_locations_refresh truncates the stage twin and atomically swaps it in), so a
--   location that drops to zero simply disappears on the next cycle with no stale row left behind.
--   The ignored-location set is still applied by the application, not here.
-- ========================================

SET NOCOUNT ON;

WITH driver_parts AS
(
    SELECT wo.PART_ID AS PartId
    FROM WORK_ORDER AS wo
    WHERE
        wo.STATUS IN ('R', 'U', 'F')
        AND wo.BASE_ID IS NOT NULL
        AND wo.PART_ID IS NOT NULL
        AND LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
        AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
        AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%'

    UNION

    SELECT req.PART_ID AS PartId
    FROM WORK_ORDER AS wo
    INNER JOIN REQUIREMENT AS req
        ON req.WORKORDER_TYPE = wo.TYPE
        AND req.WORKORDER_BASE_ID = wo.BASE_ID
        AND req.WORKORDER_LOT_ID = wo.LOT_ID
        AND req.WORKORDER_SPLIT_ID = wo.SPLIT_ID
        AND req.WORKORDER_SUB_ID = wo.SUB_ID
    WHERE
        wo.STATUS IN ('R', 'U', 'F')
        AND wo.BASE_ID IS NOT NULL
        AND LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
        AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
        AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%'
        AND req.PART_ID IS NOT NULL
)
SELECT
    part.ID AS PartNumber,
    COALESCE(NULLIF(pl.LOCATION_ID, ''), NULLIF(pl.WAREHOUSE_ID, ''), NULLIF(wh.DESCRIPTION, ''), '') AS Location,
    CAST(MAX(COALESCE(pl.QTY, part.QTY_ON_HAND, 0)) AS decimal(20, 8)) AS OnHandQuantity
FROM driver_parts AS dp
INNER JOIN PART AS part
    ON part.ID = dp.PartId
LEFT JOIN PART_LOCATION AS pl
    ON pl.PART_ID = part.ID
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = pl.WAREHOUSE_ID
WHERE
    COALESCE(NULLIF(pl.LOCATION_ID, ''), NULLIF(pl.WAREHOUSE_ID, ''), NULLIF(wh.DESCRIPTION, ''), '') <> ''
GROUP BY
    part.ID,
    COALESCE(NULLIF(pl.LOCATION_ID, ''), NULLIF(pl.WAREHOUSE_ID, ''), NULLIF(wh.DESCRIPTION, ''), '')
HAVING
    -- Only locations holding stock are cached (operator decision 2026-09-12). This is the SAME rule the
    -- caller applies (on-hand >= 1), so the mirror holds exactly the rows the application can show and
    -- a zero-quantity location never occupies a (part, location) slot.
    MAX(COALESCE(pl.QTY, part.QTY_ON_HAND, 0)) >= 1
ORDER BY
    PartNumber,
    Location;
