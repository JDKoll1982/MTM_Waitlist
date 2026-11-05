-- ========================================
-- Module: Waitlist (generic work center request, stated part)
-- Queue Script: Get Part Sequences
-- Purpose: Return every sequence Infor Visual can attribute to one part, each with the description that
--          sequence's operation carries, the operation status, whether it is the last operation the work
--          order still has to run, the resource the part goes to next, and which relation named it
--          (spec 017 FR-004, FR-005, FR-009).
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @PartNumber - the part number, matched verbatim against WORK_ORDER.PART_ID / REQUIREMENT.PART_ID
--
-- LIVE ONLY. Deliberately NOT a read shape: spec 017 FR-017 forbids substituting a cached or mirrored
-- sequence list, so this script has no entry in VisualReadShapeCatalog and no mtm_mock mirror.
--
-- HOW A SEQUENCE IS TYPED — the plant's own rule, given by IT on 2026-11-04 and matching the tag print:
--   "It reviews the ops. If it's the last op available of the work order it concludes that and points to a
--    specific print format (FG tag). Otherwise it uses WIP tag file. The Outside Service label is still the
--    WIP label ... every WIP tag puts 'Next resource' on the tag ... resource ID for O/S is 'Outside Service'."
-- So there are exactly two types: the last operation the order still has to run is the finished good, and
-- every other operation is work in progress. "Outside Service" is not a type at all — it is the name a WIP
-- tag prints on its Next resource line, because subcontracted work is routed to the OUTSIDE_SERVICE shop
-- resource. An earlier version of this script treated that resource as a type of its own, and the step then
-- told the requester that a part the floor holds 573 units of was not inventoried.
--
-- IsLastOperation is 1 when nothing later in the same work order's routing is still to be run, counting a
-- closed operation as already run. NextResource is the resource the part goes to after this operation, which
-- is the value a WIP tag shows; it is empty for the last operation, because a finished good has nowhere
-- further to go.
--
-- Two relations name a sequence, and both are answered here in one read:
--   OwnPart      WORK_ORDER.PART_ID is the part, so the sequence is on the part's own work order.
--   Requirement  REQUIREMENT.PART_ID is the part at OPERATION_SEQ_NO, so the sequence is where the part is
--                a material requirement of somebody else's operation.
-- One row per (sequence, relation); the application merges a number both relations name into one row carrying
-- both sources (FR-004). A cancelled requirement is left out of the union entirely.
--
-- ONE OPERATION SPEAKS FOR A SEQUENCE, AND IT IS THE OPEN ORDER'S. A part keeps years of work orders and its
-- routing changes between them: part 21-28841-006 runs sequence 90 at in-house press 096-03 on the order open
-- today, and ran it at OUTSIDE_SERVICE on orders closed years ago. Reading status and routing as a max across
-- that history branded a live in-house sequence as subcontracted. The facts come from the operation on the
-- part's OPEN work order (status R, U or F), and only when the part has no open order at all from the most
-- recently created closed one. GetPartSequenceState.sql applies the same rule; the two disagreeing is the
-- defect this comment exists to prevent.
--
-- OperationStatus is the code the caller classifies against RequestDispositionStatusCodes. CancelledOnly marks a
-- sequence none of whose operations is still live, which the caller does not offer (FR-005); it is measured
-- across every order, because one live operation anywhere makes the sequence real.
--
-- Description is the operation's shop resource, which is what a person reads at that sequence ("CNC LATHE").
-- ========================================

SET NOCOUNT ON;

DECLARE @PartNumberNormalized nvarchar(60) = LTRIM(RTRIM(@PartNumber));

WITH Attributed AS
(
    SELECT
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
        LTRIM(RTRIM(wo.PART_ID)) = @PartNumberNormalized
        AND o.SEQUENCE_NO IS NOT NULL

    UNION ALL

    SELECT
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
        LTRIM(RTRIM(r.PART_ID)) = @PartNumberNormalized
        AND o.SEQUENCE_NO IS NOT NULL
        AND (r.STATUS IS NULL OR r.STATUS <> 'X')
),
Ranked AS
(
    SELECT
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
            PARTITION BY a.SequenceNo, a.SequenceSource
            ORDER BY a.WorkOrderOpen DESC, a.WorkOrderCreated DESC
        ) AS RankInSequence,
        MAX(CASE WHEN a.OperationStatus IS NULL OR a.OperationStatus <> 'X' THEN 1 ELSE 0 END)
            OVER (PARTITION BY a.SequenceNo, a.SequenceSource) AS AnyNotCancelled
    FROM Attributed AS a
)
SELECT
    CAST(r.SequenceNo AS nvarchar(10)) AS Sequence,
    COALESCE(LTRIM(RTRIM(res.DESCRIPTION)), '') AS Description,
    CAST(r.OperationStatus AS nvarchar(1)) AS OperationStatus,

    -- The plant's own rule: this operation produces the finished good only when nothing later on the same work
    -- order is still to be run.
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
    r.SequenceNo,

    -- OwnPart sorts before Requirement, and the app takes the first row it sees for a number, so the part's own
    -- routing describes the sequence whenever both relations name it. Keep that ordering if this list changes.
    r.SequenceSource;
