-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Part sequences (shape 10 of 12: part_sequences)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 10 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope
-- -----
-- The per-key read (GetPartSequences.sql) attributes one part's sequences from two relations and ranks
-- them WITHIN that part. The population read is the same attribution with the part left in the
-- partition, so every part in the domain contributes exactly the rows the per-key read would return for
-- it — that is what makes the cached answer the same answer (FR-002/FR-004 transparency).
--
-- The domain is the OPEN work orders. The per-key read ranks an open order's operation ahead of a closed
-- one (WorkOrderOpen DESC, WorkOrderCreated DESC) and answers the winner, so what it actually offers is
-- the routing of the orders the plant is running. Mirroring the whole order history instead would hold
-- every sequence every part ever ran, which is not a population that can be kept current (the refresh
-- loads at most 100,000 rows). This is the same addressable-population rule work_order_search uses.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_part_sequences_refresh):
--   PartNumber, Sequence, Description, OperationStatus, IsLastOperation, NextResource, CancelledOnly,
--   SequenceSource.
--
-- Everything below the Attributed CTE — the plant's typing rule for the last operation, the next
-- resource a WIP tag prints, and CancelledOnly — is copied verbatim from GetPartSequences.sql. Only the
-- PARTITION BY clauses add the part, and only the WHERE clauses bound the domain.
-- ========================================

SET NOCOUNT ON;

WITH Attributed AS
(
    SELECT
        LTRIM(RTRIM(wo.PART_ID)) AS PartNumber,
        wo.TYPE AS WorkOrderType,
        wo.BASE_ID AS WorkOrderBaseId,
        wo.LOT_ID AS WorkOrderLotId,
        wo.SPLIT_ID AS WorkOrderSplitId,
        wo.SUB_ID AS WorkOrderSubId,
        o.SEQUENCE_NO AS SequenceNo,
        o.STATUS AS OperationStatus,
        LTRIM(RTRIM(o.RESOURCE_ID)) AS ResourceId,
        CASE WHEN wo.STATUS IN ('R', 'U', 'F') THEN 1 ELSE 0 END AS WorkOrderOpen,
        wo.CREATE_DATE AS WorkOrderCreated,
        'OwnPart' AS SequenceSource
    FROM WORK_ORDER AS wo
    INNER JOIN OPERATION AS o
        ON o.WORKORDER_TYPE = wo.TYPE
        AND o.WORKORDER_BASE_ID = wo.BASE_ID
        AND o.WORKORDER_LOT_ID = wo.LOT_ID
        AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
        AND o.WORKORDER_SUB_ID = wo.SUB_ID
    WHERE
        wo.STATUS IN ('R', 'U', 'F')
        AND NULLIF(LTRIM(RTRIM(wo.PART_ID)), '') IS NOT NULL
        AND o.SEQUENCE_NO IS NOT NULL

    UNION ALL

    SELECT
        LTRIM(RTRIM(r.PART_ID)) AS PartNumber,
        parent.TYPE AS WorkOrderType,
        parent.BASE_ID AS WorkOrderBaseId,
        parent.LOT_ID AS WorkOrderLotId,
        parent.SPLIT_ID AS WorkOrderSplitId,
        parent.SUB_ID AS WorkOrderSubId,
        o.SEQUENCE_NO AS SequenceNo,
        o.STATUS AS OperationStatus,
        LTRIM(RTRIM(o.RESOURCE_ID)) AS ResourceId,
        CASE WHEN parent.STATUS IN ('R', 'U', 'F') THEN 1 ELSE 0 END AS WorkOrderOpen,
        parent.CREATE_DATE AS WorkOrderCreated,
        'Requirement' AS SequenceSource
    FROM REQUIREMENT AS r
    INNER JOIN OPERATION AS o
        ON o.WORKORDER_TYPE = r.WORKORDER_TYPE
        AND o.WORKORDER_BASE_ID = r.WORKORDER_BASE_ID
        AND o.WORKORDER_LOT_ID = r.WORKORDER_LOT_ID
        AND o.WORKORDER_SPLIT_ID = r.WORKORDER_SPLIT_ID
        AND o.WORKORDER_SUB_ID = r.WORKORDER_SUB_ID
        AND o.SEQUENCE_NO = r.OPERATION_SEQ_NO
    INNER JOIN WORK_ORDER AS parent
        ON parent.TYPE = r.WORKORDER_TYPE
        AND parent.BASE_ID = r.WORKORDER_BASE_ID
        AND parent.LOT_ID = r.WORKORDER_LOT_ID
        AND parent.SPLIT_ID = r.WORKORDER_SPLIT_ID
        AND parent.SUB_ID = r.WORKORDER_SUB_ID
    WHERE
        parent.STATUS IN ('R', 'U', 'F')
        AND NULLIF(LTRIM(RTRIM(r.PART_ID)), '') IS NOT NULL
        AND o.SEQUENCE_NO IS NOT NULL
        AND (r.STATUS IS NULL OR r.STATUS <> 'X')
),
Ranked AS
(
    SELECT
        a.PartNumber,
        a.WorkOrderType,
        a.WorkOrderBaseId,
        a.WorkOrderLotId,
        a.WorkOrderSplitId,
        a.WorkOrderSubId,
        a.SequenceNo,
        a.OperationStatus,
        a.ResourceId,
        a.SequenceSource,
        ROW_NUMBER() OVER (
            PARTITION BY a.PartNumber, a.SequenceNo, a.SequenceSource
            ORDER BY a.WorkOrderOpen DESC, a.WorkOrderCreated DESC
        ) AS RankInSequence,
        MAX(CASE WHEN a.OperationStatus IS NULL OR a.OperationStatus <> 'X' THEN 1 ELSE 0 END)
            OVER (PARTITION BY a.PartNumber, a.SequenceNo, a.SequenceSource) AS AnyNotCancelled
    FROM Attributed AS a
)
SELECT
    r.PartNumber AS PartNumber,
    CAST(r.SequenceNo AS nvarchar(10)) AS Sequence,
    COALESCE(LTRIM(RTRIM(res.DESCRIPTION)), '') AS Description,
    CAST(r.OperationStatus AS nvarchar(1)) AS OperationStatus,

    -- The plant's own rule: this operation produces the finished good only when nothing later on the same
    -- work order is still to be run.
    CAST(CASE WHEN NEXT.NextSeq IS NULL THEN 1 ELSE 0 END AS int) AS IsLastOperation,

    -- The line a WIP tag prints. Empty for the last operation, which has nowhere further to go.
    COALESCE(LTRIM(RTRIM(NEXT.NextDescription)), '') AS NextResource,

    CAST(CASE WHEN r.AnyNotCancelled = 1 THEN 0 ELSE 1 END AS int) AS CancelledOnly,
    r.SequenceSource AS SequenceSource
FROM Ranked AS r
LEFT JOIN SHOP_RESOURCE AS res
    ON res.ID = r.ResourceId
OUTER APPLY
(
    -- The next operation this work order still has to run, which is what its tag calls the next resource.
    SELECT TOP (1)
        later.SEQUENCE_NO AS NextSeq,
        nextres.DESCRIPTION AS NextDescription
    FROM OPERATION AS later
    LEFT JOIN SHOP_RESOURCE AS nextres
        ON nextres.ID = LTRIM(RTRIM(later.RESOURCE_ID))
    WHERE later.WORKORDER_TYPE = r.WorkOrderType
        AND later.WORKORDER_BASE_ID = r.WorkOrderBaseId
        AND later.WORKORDER_LOT_ID = r.WorkOrderLotId
        AND later.WORKORDER_SPLIT_ID = r.WorkOrderSplitId
        AND later.WORKORDER_SUB_ID = r.WorkOrderSubId
        AND later.SEQUENCE_NO > r.SequenceNo
        AND later.STATUS <> 'C'
    ORDER BY later.SEQUENCE_NO
) AS NEXT
WHERE r.RankInSequence = 1
ORDER BY
    r.PartNumber,
    r.SequenceNo,

    -- OwnPart sorts before Requirement, and the app takes the first row it sees for a number, so the part's
    -- own routing describes the sequence whenever both relations name it.
    r.SequenceSource;
