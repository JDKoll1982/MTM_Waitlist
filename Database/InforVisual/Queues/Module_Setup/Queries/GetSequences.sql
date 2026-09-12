-- ========================================
-- Module: Setup
-- Queue Script: Get Sequences
-- Purpose: Resolve operation sequences for a selected work order and part
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @NormalizedWorkOrder - the `WO-######` key, matched verbatim against WORK_ORDER.BASE_ID
--   @PartNumber
-- ========================================

SET NOCOUNT ON;

-- Matches the BASE_ID verbatim: the application accepts only the `WO-######` form (operator decision
-- 2026-09-12, task T144) and passes that same normalized string to both this live read and the cached
-- copy. The former stripped-base-id fallback resolved bare `M`-family orders the application can no
-- longer ask for and could return a DIFFERENT order than the cache served (T144 collisions).
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
    wo.BASE_ID = @NormalizedWorkOrderTrimmed
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