-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Inventory locations (shape 4 of 5: inventory_locations)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 4 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope (operator decision, 2026-09-10): the parts of the open work-order population — every part
--        that is on an open order (WORK_ORDER.PART_ID) plus every part that order requires
--        (REQUIREMENT.PART_ID). "All parts" is deliberately NOT cached: the PART table is not bounded
--        by work-order activity and would put unrelated master data in the cache.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_inventory_locations_refresh):
--   PartNumber (input key AND returned part number), Location, OnHandQuantity.
--
-- One row per (part, location): the mirror carries
--   uq_visual_inventory_locations_result_location (part_number, location), and Infor Visual reports
--   inventory as ONE pooled row per part-in-location, so the per-location value is the maximum of the
--   rows GetInventoryLocations.sql would return for that part. The caller-side on-hand >= 1 filter and
--   the ignored-location set are still applied by the application, not here.
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
        AND LEN(LTRIM(RTRIM(wo.BASE_ID))) BETWEEN 5 AND 6
        AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%'

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
        AND LEN(LTRIM(RTRIM(wo.BASE_ID))) BETWEEN 5 AND 6
        AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%'
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
ORDER BY
    PartNumber,
    Location;
