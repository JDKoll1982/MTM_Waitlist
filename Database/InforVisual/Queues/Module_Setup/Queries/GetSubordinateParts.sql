-- ========================================
-- Module: Setup
-- Queue Script: Get Subordinate Parts
-- Purpose: Resolve subordinate parts for the selected work order, part, and sequence
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @NormalizedWorkOrder
--   @PartNumber
--   @SequenceNumber
-- ========================================

SET NOCOUNT ON;

DECLARE @WorkOrderBaseId nvarchar(30) =
    CASE
        WHEN CHARINDEX('-', @NormalizedWorkOrder) > 0
            THEN SUBSTRING(@NormalizedWorkOrder, CHARINDEX('-', @NormalizedWorkOrder) + 1, LEN(@NormalizedWorkOrder))
        ELSE LTRIM(RTRIM(@NormalizedWorkOrder))
    END;

DECLARE @NormalizedWorkOrderTrimmed nvarchar(30) = LTRIM(RTRIM(@NormalizedWorkOrder));

DECLARE @SequenceNumberNormalized nvarchar(20) = LTRIM(RTRIM(@SequenceNumber));
DECLARE @SequenceNumberInt smallint =
    CASE
        WHEN LEN(@SequenceNumberNormalized) > 0
            AND @SequenceNumberNormalized NOT LIKE '%[^0-9]%'
            THEN CONVERT(smallint, @SequenceNumberNormalized)
        ELSE -32768
    END;

SELECT DISTINCT
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
        -- zero-quantity PART_LOCATION rows would otherwise have yielded 'S-D0-12'.
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
-- it directly multiplied the result, and because the copies differed in Location/OnHandQuantity the
-- outermost SELECT DISTINCT could not collapse them — the Setup screen would list that subordinate
-- part once per location. Collapse to ONE row per part+warehouse, deterministically the location
-- holding the most stock and ties broken by location id, preserving single-location semantics.
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
-- used to (the extra rows share this read's dedupe key and SELECT DISTINCT cannot collapse them).
OUTER APPLY (
    SELECT TOP 1 ps.PRIMARY_LOC_ID
    FROM PART_SITE AS ps
    WHERE ps.PART_ID = req.PART_ID
        AND NULLIF(ps.PRIMARY_LOC_ID, '') IS NOT NULL
    ORDER BY ps.SITE_ID
) AS site
WHERE
    wo.BASE_ID IN (@NormalizedWorkOrderTrimmed, @WorkOrderBaseId)
    AND wo.PART_ID = @PartNumber
    AND req.OPERATION_SEQ_NO = @SequenceNumberInt
    AND req.PART_ID IS NOT NULL
ORDER BY
    Category,
    req.PART_ID;