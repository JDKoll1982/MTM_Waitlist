-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Part sequence state (shape 11 of 12: part_sequence_state)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 11 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope
-- -----
-- The per-key read (GetPartSequenceState.sql) answers one part's operation state at one sequence, taken
-- from the open order's operation that speaks for that sequence. The population read is the same
-- attribution reduced to one row per (part, sequence) — the partition gains the part, and the rank keeps
-- the open order's operation ahead of a closed one — so every part in the domain contributes exactly the
-- row the per-key read would return for it (FR-002/FR-004 transparency).
--
-- The domain is the OPEN work orders, the same addressable population shape 10 mirrors, so the state and
-- the list a requester chooses from cannot describe different orders.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_part_sequence_state_refresh):
--   PartNumber, Sequence, OperationStatus, IsLastOperation, NextResource.
--
-- Everything from the Attributed CTE down — the open order's operation winning, the plant's typing rule
-- for the last operation, and the next resource a WIP tag prints — is copied verbatim from
-- GetPartSequenceState.sql. Only the PARTITION BY clauses add the part, and only the WHERE clauses bound
-- the domain.
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
        wo.CREATE_DATE AS WorkOrderCreated
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
        parent.CREATE_DATE AS WorkOrderCreated
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
        ROW_NUMBER() OVER (
            PARTITION BY a.PartNumber, a.SequenceNo
            ORDER BY a.WorkOrderOpen DESC, a.WorkOrderCreated DESC
        ) AS RankInSequence
    FROM Attributed AS a
)
SELECT
    r.PartNumber AS PartNumber,
    CAST(r.SequenceNo AS nvarchar(20)) AS Sequence,
    CAST(r.OperationStatus AS nvarchar(1)) AS OperationStatus,
    CAST(CASE WHEN NEXT.NextSeq IS NULL THEN 1 ELSE 0 END AS int) AS IsLastOperation,
    COALESCE(LTRIM(RTRIM(NEXT.NextDescription)), '') AS NextResource
FROM Ranked AS r
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
    r.SequenceNo;
