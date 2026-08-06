-- ========================================
-- Module: Setup
-- Queue Script: Get Sequences
-- Purpose: Resolve operation sequences for a selected work order and part
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @NormalizedWorkOrder
--   @PartNumber
-- ========================================

SET NOCOUNT ON;

DECLARE @WorkOrderBaseId nvarchar(30) =
    CASE
        WHEN CHARINDEX('-', @NormalizedWorkOrder) > 0
            THEN SUBSTRING(@NormalizedWorkOrder, CHARINDEX('-', @NormalizedWorkOrder) + 1, LEN(@NormalizedWorkOrder))
        ELSE LTRIM(RTRIM(@NormalizedWorkOrder))
    END;

DECLARE @NormalizedWorkOrderTrimmed nvarchar(30) = LTRIM(RTRIM(@NormalizedWorkOrder));

SELECT DISTINCT
    o.SEQUENCE_NO AS SequenceNumber,
    CONCAT('Operation ', o.SEQUENCE_NO, ' / ', COALESCE(NULLIF(o.RESOURCE_ID, ''), 'Unassigned')) AS Description
FROM WORK_ORDER AS wo
INNER JOIN OPERATION AS o
    ON o.WORKORDER_TYPE = wo.TYPE
    AND o.WORKORDER_BASE_ID = wo.BASE_ID
    AND o.WORKORDER_LOT_ID = wo.LOT_ID
    AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
    AND o.WORKORDER_SUB_ID = wo.SUB_ID
WHERE
    wo.BASE_ID IN (@NormalizedWorkOrderTrimmed, @WorkOrderBaseId)
    AND wo.PART_ID = @PartNumber
    AND EXISTS
    (
        SELECT 1
        FROM REQUIREMENT AS req
        WHERE req.WORKORDER_TYPE = wo.TYPE
            AND req.WORKORDER_BASE_ID = wo.BASE_ID
            AND req.WORKORDER_LOT_ID = wo.LOT_ID
            AND req.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND req.WORKORDER_SUB_ID = wo.SUB_ID
            AND req.OPERATION_SEQ_NO = o.SEQUENCE_NO
            AND req.PART_ID IS NOT NULL
    )
ORDER BY
    o.SEQUENCE_NO;