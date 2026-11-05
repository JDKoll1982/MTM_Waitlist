-- ========================================
-- Module: Waitlist (generic work center request, stated part)
-- Queue Script: Get Part Sequence State
-- Purpose: Re-read one sequence's operation state for one part at the moment the quantity is asked for, so
--          the owner of the quantity and the type of the sequence are decided from the state now and not
--          from what the list carried a minute ago (spec 017 FR-009, research R4).
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @PartNumber - the part number, matched verbatim against WORK_ORDER.PART_ID / REQUIREMENT.PART_ID
--   @Sequence   - the sequence number the requester chose
--
-- LIVE ONLY. Deliberately NOT a read shape: FR-017 forbids substituting a cached or mirrored quantity, and
-- the state that decides who owns the quantity must be read at the same moment as the quantity.
--
-- IsLastOperation carries the plant's own typing rule, given by IT on 2026-11-04: the last operation the work
-- order still has to run produces the finished good, and every other operation is work in progress. A WIP tag
-- prints the resource the part goes to next, and "Outside Service" is one of those next-resource names rather
-- than a type of its own, because subcontracted work is routed to the OUTSIDE_SERVICE shop resource. An
-- earlier version of this script treated that resource as a type, and the step told the requester that a part
-- the WIP floor holds 573 units of was not inventoried.
--
-- WHO OWNS THE QUANTITY is still the operation's state, not its type: an operation still open has its material
-- on the floor by part and sequence, and one already closed has it in Infor Visual by part number (FR-009).
-- NextResource is returned because it is what a person reads on the card; it never decides an owner.
--
-- THE OPEN ORDER'S OPERATION SPEAKS FOR THE SEQUENCE, exactly as GetPartSequences.sql does, so a closed order's
-- routing cannot decide a sequence the part is running today.
--
-- No row is returned when the part has no operation at that sequence at all, which the caller reads as "no state
-- recorded" and answers by asking the floor first (FR-011). That is deliberately not the same as an operation
-- that exists with a null status.
-- ========================================

SET NOCOUNT ON;

DECLARE @PartNumberNormalized nvarchar(60) = LTRIM(RTRIM(@PartNumber));
DECLARE @SequenceText nvarchar(20) = LTRIM(RTRIM(@Sequence));
DECLARE @SequenceNumber int = NULL;

-- A sequence the caller could not read as a number names no operation, so it matches nothing and no row comes
-- back, which the caller already treats as unrecorded. Guarding the cast this way keeps a bad value from throwing
-- on a server older than TRY_CONVERT (this one is at compatibility level 100).
IF @SequenceText <> '' AND @SequenceText NOT LIKE '%[^0-9]%'
    SET @SequenceNumber = CAST(@SequenceText AS int);

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
        wo.CREATE_DATE AS WorkOrderCreated
    FROM WORK_ORDER AS wo
    INNER JOIN OPERATION AS o
        ON o.WORKORDER_TYPE = wo.TYPE
        AND o.WORKORDER_BASE_ID = wo.BASE_ID
        AND o.WORKORDER_LOT_ID = wo.LOT_ID
        AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
        AND o.WORKORDER_SUB_ID = wo.SUB_ID
    WHERE
        LTRIM(RTRIM(wo.PART_ID)) = @PartNumberNormalized
        AND o.SEQUENCE_NO = @SequenceNumber

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
        LTRIM(RTRIM(r.PART_ID)) = @PartNumberNormalized
        AND o.SEQUENCE_NO = @SequenceNumber
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
        ROW_NUMBER() OVER (
            ORDER BY a.WorkOrderOpen DESC, a.WorkOrderCreated DESC
        ) AS RankInSequence
    FROM Attributed AS a
)
SELECT TOP (1)
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
WHERE r.RankInSequence = 1;
