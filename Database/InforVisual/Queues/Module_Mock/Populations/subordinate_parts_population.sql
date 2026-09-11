-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Subordinate parts (shape 3 of 5: subordinate_parts)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 3 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope: for every ADDRESSABLE open work order (STATUS in 'R','U','F'; see the addressability guard
--        below), every requirement row of every one of its operations — exactly the rows
--        GetSubordinateParts.sql returns for the (work order, order part, operation sequence) triples
--        that shape 2 lists.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_subordinate_parts_refresh):
--   NormalizedWorkOrder, ParentPartNumber, SequenceNumber (input keys),
--   Category, PartNumber, Description, Location, User8, OnHandQuantity (outputs).
--   `PartNumber` in a returned row is always the SUBORDINATE part, while `ParentPartNumber` carries
--   the order's own part number the caller passes in as its `PartNumber` parameter.
-- ========================================

SET NOCOUNT ON;

SELECT DISTINCT
    CASE
        WHEN LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%' THEN LTRIM(RTRIM(wo.BASE_ID))
        ELSE 'WO-' + LTRIM(RTRIM(wo.BASE_ID))
    END AS NormalizedWorkOrder,
    wo.PART_ID AS ParentPartNumber,
    req.OPERATION_SEQ_NO AS SequenceNumber,
    CASE
        WHEN req.PART_ID LIKE 'MMC%' THEN 'Coil'
        WHEN req.PART_ID LIKE 'FGT%' THEN 'Die'
        WHEN req.PART_ID LIKE 'MMF%' THEN 'Flatstock'
        ELSE 'Component'
    END AS Category,
    req.PART_ID AS PartNumber,
    CASE
        WHEN req.PART_ID LIKE 'FGT%'
            AND COALESCE(part.DESCRIPTION, '') = 'Die still needs location'
            THEN 'No Die'
        ELSE COALESCE(part.DESCRIPTION, '')
    END AS Description,
    CASE
        WHEN req.PART_ID LIKE 'MMC%' THEN COALESCE(NULLIF(req.LOCATION_ID, ''), NULLIF(pl.LOCATION_ID, ''), NULLIF(wh.DESCRIPTION, ''), '')
        -- A die is not inventoried: PART_SITE.QTY_ON_HAND is 0 and every PART_LOCATION row for it carries
        -- QTY 0, so ranking those rows by quantity is meaningless and picked an arbitrary location
        -- (alphabetically first among several zero-quantity rows). A die's real home location is the part's
        -- PRIMARY location. Verified against MTMFG: FGT0602-01 has PRIMARY_LOC_ID 'U-B0-01', while its six
        -- zero-quantity PART_LOCATION rows would otherwise have yielded 'S-D0-12'. This must stay identical
        -- to Module_Setup/Queries/GetSubordinateParts.sql so the mirror matches the live read.
        WHEN req.PART_ID LIKE 'FGT%' THEN COALESCE(NULLIF(site.PRIMARY_LOC_ID, ''), NULLIF(req.LOCATION_ID, ''), '')
        WHEN req.PART_ID LIKE 'MMF%' THEN COALESCE(NULLIF(req.LOCATION_ID, ''), NULLIF(pl.LOCATION_ID, ''), NULLIF(wh.DESCRIPTION, ''), '')
        ELSE ''
    END AS Location,
    COALESCE(NULLIF(part.USER_8, ''), '') AS User8,
    CAST(COALESCE(pl.QTY, part.QTY_ON_HAND, 0) AS decimal(20, 8)) AS OnHandQuantity
FROM WORK_ORDER AS wo
INNER JOIN REQUIREMENT AS req
    ON req.WORKORDER_TYPE = wo.TYPE
    AND req.WORKORDER_BASE_ID = wo.BASE_ID
    AND req.WORKORDER_LOT_ID = wo.LOT_ID
    AND req.WORKORDER_SPLIT_ID = wo.SPLIT_ID
    AND req.WORKORDER_SUB_ID = wo.SUB_ID
LEFT JOIN PART AS part
    ON part.ID = req.PART_ID
-- PART_LOCATION carries one row per STORAGE LOCATION, so a part stocked in several locations within
-- one warehouse appears many times (one part has 26 rows in warehouse 002, 25 of them empty). Joining
-- it directly multiplied the result: the extra rows shared this shape's mirror key
-- (work_order, parent_part, sequence, part_number) while differing in Location/OnHandQuantity, so
-- SELECT DISTINCT could not collapse them and the refresh aborted with
-- "Duplicate entry ... for key 'uq_visual_subordinate_parts_result_lookup_key'".
-- Collapse to ONE row per part+warehouse, deterministically the location holding the most stock and
-- ties broken by location id, which keeps the single-location semantics the live read returns.
LEFT JOIN (
    SELECT
        PART_ID,
        WAREHOUSE_ID,
        LOCATION_ID,
        QTY,
        ROW_NUMBER() OVER (
            PARTITION BY PART_ID, WAREHOUSE_ID
            ORDER BY QTY DESC, LOCATION_ID ASC) AS stock_rank
    FROM PART_LOCATION
) AS pl
    ON pl.PART_ID = req.PART_ID
    AND pl.WAREHOUSE_ID = req.WAREHOUSE_ID
    AND pl.stock_rank = 1
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = COALESCE(req.WAREHOUSE_ID, pl.WAREHOUSE_ID)
-- A die's home location is its PRIMARY location, which lives on PART_SITE, not on a stock row.
-- OUTER APPLY with TOP 1 keeps this to a single row per requirement: a part can carry a PART_SITE row per
-- site, so joining the table directly would multiply the results exactly the way the PART_LOCATION join
-- used to (the extra rows share this shape's mirror key and SELECT DISTINCT cannot collapse them).
OUTER APPLY (
    SELECT TOP 1 ps.PRIMARY_LOC_ID
    FROM PART_SITE AS ps
    WHERE ps.PART_ID = req.PART_ID
        AND NULLIF(ps.PRIMARY_LOC_ID, '') IS NOT NULL
    ORDER BY ps.SITE_ID
) AS site
WHERE
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    -- Addressable work orders only: the two forms the application can ask for AND the live read
    -- (GetSubordinateParts.sql) resolves. Full rationale in work_order_lookup_population.sql.
    AND (
        (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
         AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
         AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%')
        OR
        (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 6
         AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%')
    )
    AND req.PART_ID IS NOT NULL
ORDER BY
    NormalizedWorkOrder,
    ParentPartNumber,
    SequenceNumber,
    Category,
    PartNumber;
