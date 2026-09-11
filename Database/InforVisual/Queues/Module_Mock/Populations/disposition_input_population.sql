-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Disposition input (shape 5 of 5: disposition_input)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 5 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope: every ADDRESSABLE open work order (STATUS in 'R','U','F'; see the addressability guard
--        below) paired with its own order part — exactly the (work order, part) pairs
--        RequestDispositionResolver asks about for a work centre's active setup job.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_disposition_input_refresh):
--   WorkOrder, PartNumber (input keys),
--   WorkOrderStatus, OpenWorkOrderQuantity, FinishedGoodsQuantity, HasOutsideVendorOperation (outputs).
--
-- `WorkOrderStatus` is the raw Visual status code and the KEY is the same normalized `WO-` form the
-- caller uses. The status MEANING stays owned solely by RequestDispositionStatusCodes; this cache
-- never interprets it (FR-018).
-- ========================================

SET NOCOUNT ON;

SELECT
    CASE
        WHEN LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%' THEN LTRIM(RTRIM(wo.BASE_ID))
        ELSE 'WO-' + LTRIM(RTRIM(wo.BASE_ID))
    END AS WorkOrder,
    wo.PART_ID AS PartNumber,
    LTRIM(RTRIM(wo.STATUS)) AS WorkOrderStatus,
    CAST(
        CASE WHEN wo.DESIRED_QTY > wo.RECEIVED_QTY THEN wo.DESIRED_QTY - wo.RECEIVED_QTY ELSE 0 END
        AS decimal(20, 8)
    ) AS OpenWorkOrderQuantity,
    CAST(COALESCE(part.QTY_ON_HAND, 0) AS decimal(20, 8)) AS FinishedGoodsQuantity,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM OPERATION AS o
        WHERE o.WORKORDER_TYPE = wo.TYPE
            AND o.WORKORDER_BASE_ID = wo.BASE_ID
            AND o.WORKORDER_LOT_ID = wo.LOT_ID
            AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND o.WORKORDER_SUB_ID = wo.SUB_ID
            AND (LTRIM(RTRIM(ISNULL(o.VENDOR_ID, ''))) <> ''
                 OR LTRIM(RTRIM(ISNULL(o.SERVICE_ID, ''))) <> ''
                 OR LTRIM(RTRIM(ISNULL(o.SERVICE_PART_ID, ''))) <> '')
    ) THEN 1 ELSE 0 END AS HasOutsideVendorOperation
FROM WORK_ORDER AS wo
LEFT JOIN PART AS part
    ON part.ID = wo.PART_ID
WHERE
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    -- Addressable work orders only: the two forms the application can ask for AND the live read
    -- (GetDispositionInput.sql) resolves. Full rationale in work_order_lookup_population.sql.
    AND (
        (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
         AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
         AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%')
        OR
        (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 6
         AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%')
    )
ORDER BY
    WorkOrder,
    PartNumber;
