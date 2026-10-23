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
--   WorkOrder, PartNumber, WorkCenter (input keys),
--   WorkOrderStatus, OpenWorkOrderQuantity, FinishedGoodsQuantity, HasOutsideVendorOperation,
--   NextOperationResourceId, HasRemainingOperation (outputs).
--
-- `WorkCenter` is part of the key because the routing answer is anchored on it: the operation at that work
-- centre is the job's own, so "what follows" differs per work centre. The read therefore enumerates every
-- (work order, part, work centre) triple the order's own routing names, which is what makes a cached answer
-- identical to the live one for the same question.
--
-- `NextOperationResourceId` / `HasRemainingOperation` are the routing, computed exactly as the live read
-- (GetDispositionInput.sql) computes them: the first open operation after the job's own. NULL in both means the
-- routing could not be anchored (the work centre is not on the order), which the classifier reads as "routing
-- not read" rather than "no operation remains" (research 2026-10-23).
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

;WITH addressable AS
(
    SELECT
        wo.TYPE,
        LTRIM(RTRIM(wo.BASE_ID)) AS WorkOrder,
        wo.BASE_ID,
        wo.LOT_ID,
        wo.SPLIT_ID,
        wo.SUB_ID,
        wo.PART_ID,
        LTRIM(RTRIM(wo.STATUS)) AS WorkOrderStatus,
        wo.DESIRED_QTY,
        wo.RECEIVED_QTY
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
),
job_operations AS
(
    SELECT DISTINCT
        a.TYPE,
        a.BASE_ID,
        a.LOT_ID,
        a.SPLIT_ID,
        a.SUB_ID,
        a.PART_ID,
        a.WorkOrder,
        a.WorkOrderStatus,
        a.DESIRED_QTY,
        a.RECEIVED_QTY,
        LTRIM(RTRIM(o.RESOURCE_ID)) AS WorkCenter
    FROM addressable AS a
    INNER JOIN OPERATION AS o
        ON o.WORKORDER_TYPE = a.TYPE
        AND o.WORKORDER_BASE_ID = a.BASE_ID
        AND o.WORKORDER_LOT_ID = a.LOT_ID
        AND o.WORKORDER_SPLIT_ID = a.SPLIT_ID
        AND o.WORKORDER_SUB_ID = a.SUB_ID
    WHERE LTRIM(RTRIM(ISNULL(o.RESOURCE_ID, ''))) <> ''
),
anchored AS
(
    SELECT
        j.*,
        COALESCE(
            (
                SELECT MIN(o2.SEQUENCE_NO)
                FROM OPERATION AS o2
                WHERE o2.WORKORDER_TYPE = j.TYPE
                    AND o2.WORKORDER_BASE_ID = j.BASE_ID
                    AND o2.WORKORDER_LOT_ID = j.LOT_ID
                    AND o2.WORKORDER_SPLIT_ID = j.SPLIT_ID
                    AND o2.WORKORDER_SUB_ID = j.SUB_ID
                    AND LTRIM(RTRIM(o2.RESOURCE_ID)) = j.WorkCenter
                    AND o2.STATUS <> 'C'
            ),
            (
                SELECT MIN(o3.SEQUENCE_NO)
                FROM OPERATION AS o3
                WHERE o3.WORKORDER_TYPE = j.TYPE
                    AND o3.WORKORDER_BASE_ID = j.BASE_ID
                    AND o3.WORKORDER_LOT_ID = j.LOT_ID
                    AND o3.WORKORDER_SPLIT_ID = j.SPLIT_ID
                    AND o3.WORKORDER_SUB_ID = j.SUB_ID
                    AND LTRIM(RTRIM(o3.RESOURCE_ID)) = j.WorkCenter
            )
        ) AS CurrentSequence
    FROM job_operations AS j
)
SELECT
    a.WorkOrder,
    a.PART_ID AS PartNumber,
    a.WorkCenter,
    a.WorkOrderStatus,
    CAST(
        CASE WHEN a.DESIRED_QTY > a.RECEIVED_QTY THEN a.DESIRED_QTY - a.RECEIVED_QTY ELSE 0 END
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
        WHERE pl.PART_ID = a.PART_ID
            AND (ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                 OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                 OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG'))
    ), 0) AS decimal(20, 8)) AS FinishedGoodsQuantity,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM OPERATION AS o
        WHERE o.WORKORDER_TYPE = a.TYPE
            AND o.WORKORDER_BASE_ID = a.BASE_ID
            AND o.WORKORDER_LOT_ID = a.LOT_ID
            AND o.WORKORDER_SPLIT_ID = a.SPLIT_ID
            AND o.WORKORDER_SUB_ID = a.SUB_ID
            AND (LTRIM(RTRIM(ISNULL(o.VENDOR_ID, ''))) <> ''
                 OR LTRIM(RTRIM(ISNULL(o.SERVICE_ID, ''))) <> ''
                 OR LTRIM(RTRIM(ISNULL(o.SERVICE_PART_ID, ''))) <> '')
    ) THEN 1 ELSE 0 END AS HasOutsideVendorOperation,
    CASE WHEN a.CurrentSequence IS NULL THEN NULL ELSE
    (
        SELECT TOP (1) LTRIM(RTRIM(o4.RESOURCE_ID))
        FROM OPERATION AS o4
        WHERE o4.WORKORDER_TYPE = a.TYPE
            AND o4.WORKORDER_BASE_ID = a.BASE_ID
            AND o4.WORKORDER_LOT_ID = a.LOT_ID
            AND o4.WORKORDER_SPLIT_ID = a.SPLIT_ID
            AND o4.WORKORDER_SUB_ID = a.SUB_ID
            AND o4.SEQUENCE_NO > a.CurrentSequence
            AND o4.STATUS <> 'C'
        ORDER BY o4.SEQUENCE_NO
    ) END AS NextOperationResourceId,
    CASE WHEN a.CurrentSequence IS NULL THEN NULL ELSE
    (
        CASE WHEN EXISTS
        (
            SELECT 1
            FROM OPERATION AS o5
            WHERE o5.WORKORDER_TYPE = a.TYPE
                AND o5.WORKORDER_BASE_ID = a.BASE_ID
                AND o5.WORKORDER_LOT_ID = a.LOT_ID
                AND o5.WORKORDER_SPLIT_ID = a.SPLIT_ID
                AND o5.WORKORDER_SUB_ID = a.SUB_ID
                AND o5.SEQUENCE_NO > a.CurrentSequence
                AND o5.STATUS <> 'C'
        ) THEN 1 ELSE 0 END
    ) END AS HasRemainingOperation
FROM anchored AS a
ORDER BY
    a.WorkOrder,
    a.PART_ID,
    a.WorkCenter;
