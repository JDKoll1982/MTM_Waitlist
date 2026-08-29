SET NOCOUNT ON;

DECLARE @ActivePartStatus nchar(1) = 'A';
DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

SELECT TOP (25)
    CASE
        WHEN wo.BASE_ID LIKE 'WO-%' THEN CONCAT(wo.BASE_ID, '-', wo.LOT_ID, '-', wo.SPLIT_ID, '-', wo.SUB_ID)
        ELSE CONCAT(wo.TYPE, '-', wo.BASE_ID, '-', wo.LOT_ID, '-', wo.SPLIT_ID, '-', wo.SUB_ID)
    END

AS WorkOrderId,
wo.TYPE AS WorkOrderType,
wo.BASE_ID AS WorkOrderBaseId,
wo.LOT_ID AS WorkOrderLotId,
wo.SPLIT_ID AS WorkOrderSplitId,
wo.SUB_ID AS WorkOrderSubId,
CASE
    WHEN wo.BASE_ID LIKE 'WO-%' THEN 'WO Floor Job'
    ELSE 'Work Order'
END AS RequestType,
wo.PART_ID AS PartNumber,
part.DESCRIPTION AS PartDescription,
'Fabricated' AS PartKind,
wo.DESIRED_QTY AS DesiredQuantity,
wo.ALLOCATED_QTY AS AllocatedQuantity,
wo.FULFILLED_QTY AS FulfilledQuantity,
CASE
    WHEN wo.DESIRED_QTY - wo.FULFILLED_QTY < 0 THEN 0
    ELSE wo.DESIRED_QTY - wo.FULFILLED_QTY
END AS RemainingQuantity,
wo.STATUS AS WorkOrderStatus,
wo.CREATE_DATE AS CreateDate,
wo.DESIRED_RLS_DATE AS DesiredReleaseDate,
wo.DESIRED_WANT_DATE AS DesiredWantDate,
wo.SCHED_START_DATE AS ScheduledStart,
wo.SCHED_FINISH_DATE AS ScheduledFinish,
wo.PLANNER_ID AS PlannerId,
wo.ENGINEERED_BY AS EngineeredBy,
op.SEQUENCE_NO AS OperationSequence,
op.RESOURCE_ID AS PressOrResourceId,
sr.DESCRIPTION AS PressOrResourceDescription,
op.SETUP_HRS AS SetupHours,
op.RUN_HRS AS RunHours,
op.STATUS AS OperationStatus,
op.COMPLETED_QTY AS OperationCompletedQuantity
FROM
    WORK_ORDER AS wo
    INNER JOIN PART AS part ON part.ID = wo.PART_ID OUTER APPLY (
        SELECT TOP (1) o.SEQUENCE_NO, o.RESOURCE_ID, o.SETUP_HRS, o.RUN_HRS, o.STATUS, o.COMPLETED_QTY
        FROM OPERATION AS o
        WHERE
            o.WORKORDER_TYPE = wo.TYPE
            AND o.WORKORDER_BASE_ID = wo.BASE_ID
            AND o.WORKORDER_LOT_ID = wo.LOT_ID
            AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND o.WORKORDER_SUB_ID = wo.SUB_ID
        ORDER BY o.SEQUENCE_NO
    ) AS op
    LEFT JOIN SHOP_RESOURCE AS sr ON sr.ID = op.RESOURCE_ID
WHERE
    wo.CREATE_DATE >= @CutoffDate
    AND COALESCE(
        NULLIF(part.STATUS, ''),
        @ActivePartStatus
    ) = @ActivePartStatus
    AND wo.BASE_ID LIKE 'WO-%'
    AND wo.CLOSE_DATE IS NULL
    AND wo.DESIRED_QTY > wo.FULFILLED_QTY
ORDER BY COALESCE(
        wo.SCHED_FINISH_DATE, wo.DESIRED_RLS_DATE, wo.CREATE_DATE
    ), wo.BASE_ID, op.SEQUENCE_NO;