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
-- `FinishedGoodsQuantity` is the part's stock AT A FINISHED-GOODS LOCATION, projected exactly as the live read
-- (GetDispositionInput.sql) projects it, so a cached answer and a live answer cannot differ in shape or meaning
-- (FR-090). It is NOT PART.QTY_ON_HAND: that is the part's total on-hand across every location with no location
-- filter, and it is the defect the spec records. The finished-goods class is
-- LOCATION.DESCRIPTION LIKE '%FINISHED GOODS%' OR WAREHOUSE.DESCRIPTION LIKE '%FINISHED GOODS%'
-- OR LOCATION.ID IN ('FG','DC-FG'), mirroring the taxonomy documented for the WIP side in
-- Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql. The codes are NOT YET CONFIRMED for
-- this installation: task T067's live validation (Database/Validation/disposition_input_finished_goods/validate.sql)
-- confirms them, and none is invented before it does.
--
-- `WorkOrderStatus` is the raw Visual status code and the KEY is the same normalized `WO-` form the
-- caller uses. The status MEANING stays owned solely by RequestDispositionStatusCodes; this cache
-- never interprets it (FR-018).
-- ========================================

SET NOCOUNT ON;

SELECT
    LTRIM(RTRIM(wo.BASE_ID)) AS WorkOrder,
    wo.PART_ID AS PartNumber,
    LTRIM(RTRIM(wo.STATUS)) AS WorkOrderStatus,
    CAST(
        CASE WHEN wo.DESIRED_QTY > wo.RECEIVED_QTY THEN wo.DESIRED_QTY - wo.RECEIVED_QTY ELSE 0 END
        AS decimal(20, 8)
    ) AS OpenWorkOrderQuantity,
    CAST(COALESCE(
    (
        SELECT SUM(COALESCE(pl.QTY, 0))
        FROM PART_LOCATION AS pl
        LEFT JOIN WAREHOUSE AS wh
            ON wh.ID = pl.WAREHOUSE_ID
        LEFT JOIN LOCATION AS loc
            ON loc.WAREHOUSE_ID = pl.WAREHOUSE_ID
            AND loc.ID = pl.LOCATION_ID
        WHERE pl.PART_ID = wo.PART_ID
            AND (ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                 OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                 OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG'))
    ), 0) AS decimal(20, 8)) AS FinishedGoodsQuantity,
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
WHERE
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    -- Addressable work orders only (operator decision 2026-09-12, task T144): the application sends only
    -- the canonical `WO-######` key AND the live read (GetDispositionInput.sql) resolves it verbatim, so
    -- BASE_ID must literally be `WO-` + 6 digits. Full rationale in work_order_lookup_population.sql.
    AND LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
    AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
    AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%'
ORDER BY
    WorkOrder,
    PartNumber;
