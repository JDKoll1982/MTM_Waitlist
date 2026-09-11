-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Subordinate parts (shape 3 of 5: subordinate_parts)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 3 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope: for every OPEN work order (STATUS in 'R','U','F'), every requirement row of every one of its
--        operations — exactly the rows GetSubordinateParts.sql returns for the
--        (work order, order part, operation sequence) triples that shape 2 lists.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_subordinate_parts_refresh):
--   NormalizedWorkOrder, ParentPartNumber, SequenceNumber (input keys),
--   Category, PartNumber, Description, Location, User8, OnHandQuantity (outputs).
--   `PartNumber` in a returned row is always the SUBORDINATE part, while `ParentPartNumber` carries
--   the order's own part number the caller passes in as its `PartNumber` parameter.
-- ========================================

SET NOCOUNT ON;

SELECT DISTINCT
    'WO-' + RIGHT('000000' + LTRIM(RTRIM(wo.BASE_ID)), 6) AS NormalizedWorkOrder,
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
        WHEN req.PART_ID LIKE 'FGT%' THEN COALESCE(NULLIF(req.LOCATION_ID, ''), NULLIF(pl.LOCATION_ID, ''), NULLIF(wh.DESCRIPTION, ''), '')
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
LEFT JOIN PART_LOCATION AS pl
    ON pl.PART_ID = req.PART_ID
    AND pl.WAREHOUSE_ID = req.WAREHOUSE_ID
LEFT JOIN WAREHOUSE AS wh
    ON wh.ID = COALESCE(req.WAREHOUSE_ID, pl.WAREHOUSE_ID)
WHERE
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    AND LEN(LTRIM(RTRIM(wo.BASE_ID))) BETWEEN 5 AND 6
    AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%'
    AND req.PART_ID IS NOT NULL
ORDER BY
    NormalizedWorkOrder,
    ParentPartNumber,
    SequenceNumber,
    Category,
    PartNumber;
