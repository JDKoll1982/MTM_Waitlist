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
        WHEN req.PART_ID LIKE 'FGT%' THEN COALESCE(NULLIF(req.LOCATION_ID, ''), NULLIF(pl.LOCATION_ID, ''), NULLIF(wh.DESCRIPTION, ''), '')
        WHEN req.PART_ID LIKE 'MMF%' THEN COALESCE(NULLIF(req.LOCATION_ID, ''), NULLIF(pl.LOCATION_ID, ''), NULLIF(wh.DESCRIPTION, ''), '')
        ELSE ''
    END AS Location,
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
LEFT JOIN PART_LOCATION AS pl
    ON pl.PART_ID = req.PART_ID
    AND pl.WAREHOUSE_ID = req.WAREHOUSE_ID
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = COALESCE(req.WAREHOUSE_ID, pl.WAREHOUSE_ID)
WHERE
    wo.BASE_ID IN (@NormalizedWorkOrderTrimmed, @WorkOrderBaseId)
    AND wo.PART_ID = @PartNumber
    AND req.OPERATION_SEQ_NO = @SequenceNumberInt
    AND req.PART_ID IS NOT NULL
ORDER BY
    Category,
    req.PART_ID;