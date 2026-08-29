SET NOCOUNT ON;

DECLARE @ActivePartStatus nchar(1) = 'A';
DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

;WITH OpenCustomerOrderLines AS
(
    SELECT TOP (25)
        co.ID AS CustomerOrderId,
        col.LINE_NO AS LineNumber,
        COALESCE(
            NULLIF(NULLIF(col.ORDER_TYPE, ''), 'Unknown'),
            NULLIF(NULLIF(co.ORDER_TYPE, ''), 'Unknown'),
            NULLIF(NULLIF(col.TYPE, ''), 'Unknown'),
            'Fabricated'
        ) AS RequestType,
        col.PART_ID AS PartNumber,
        part.DESCRIPTION AS PartDescription,
        COALESCE(co.PROMISE_DATE, co.DESIRED_SHIP_DATE, col.DESIRED_SHIP_DATE) AS DueDate,
        col.ORDER_QTY,
        col.FULFILLED_QTY,
        CASE
            WHEN col.ORDER_QTY - col.FULFILLED_QTY < 0 THEN 0
            ELSE col.ORDER_QTY - col.FULFILLED_QTY
        END

AS RemainingQuantity
    FROM CUSTOMER_ORDER AS co
    INNER JOIN CUST_ORDER_LINE AS col
        ON col.CUST_ORDER_ID = co.ID
    INNER JOIN PART AS part
        ON part.ID = col.PART_ID
    WHERE
        co.ORDER_DATE >= @CutoffDate
        AND COALESCE(NULLIF(part.STATUS, ''), @ActivePartStatus) = @ActivePartStatus
        AND part.DETAIL_ONLY = 'N'
        AND part.INVENTORY_LOCKED = 'N'
        AND part.FABRICATED = 'Y'
        AND col.ORDER_QTY > col.FULFILLED_QTY
),
OpenWorkOrders AS
(
    SELECT TOP (25)
        CASE
            WHEN wo.BASE_ID LIKE 'WO-%' THEN CONCAT(wo.BASE_ID, '-', wo.LOT_ID, '-', wo.SPLIT_ID, '-', wo.SUB_ID)
            ELSE CONCAT(wo.TYPE, '-', wo.BASE_ID, '-', wo.LOT_ID, '-', wo.SPLIT_ID, '-', wo.SUB_ID)
        END AS WorkOrderId,
        wo.PART_ID AS PartNumber,
        part.DESCRIPTION AS PartDescription,
        wo.DESIRED_QTY,
        wo.FULFILLED_QTY,
        CASE
            WHEN wo.DESIRED_QTY - wo.FULFILLED_QTY < 0 THEN 0
            ELSE wo.DESIRED_QTY - wo.FULFILLED_QTY
        END AS RemainingQuantity,
        COALESCE(wo.SCHED_FINISH_DATE, wo.DESIRED_RLS_DATE, wo.CREATE_DATE) AS DueDate,
        op.RESOURCE_ID AS PressOrResourceId,
        sr.DESCRIPTION AS PressOrResourceDescription,
        op.SEQUENCE_NO AS OperationSequence
    FROM WORK_ORDER AS wo
    INNER JOIN PART AS part
        ON part.ID = wo.PART_ID
    OUTER APPLY
    (
        SELECT TOP (1)
            o.SEQUENCE_NO,
            o.RESOURCE_ID
        FROM OPERATION AS o
        WHERE
            o.WORKORDER_TYPE = wo.TYPE
            AND o.WORKORDER_BASE_ID = wo.BASE_ID
            AND o.WORKORDER_LOT_ID = wo.LOT_ID
            AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND o.WORKORDER_SUB_ID = wo.SUB_ID
        ORDER BY o.SEQUENCE_NO
    ) AS op
    LEFT JOIN SHOP_RESOURCE AS sr
        ON sr.ID = op.RESOURCE_ID
    WHERE
        wo.CREATE_DATE >= @CutoffDate
        AND COALESCE(NULLIF(part.STATUS, ''), @ActivePartStatus) = @ActivePartStatus
        AND part.DETAIL_ONLY = 'N'
        AND part.INVENTORY_LOCKED = 'N'
        AND part.FABRICATED = 'Y'
        AND wo.BASE_ID LIKE 'WO-%'
        AND wo.CLOSE_DATE IS NULL
        AND wo.DESIRED_QTY > wo.FULFILLED_QTY
        AND ISNULL(op.RESOURCE_ID, '') NOT IN ('500-GRP', 'OUTSIDE_SERVICE', 'Materials')
        AND ISNULL(sr.DESCRIPTION, '') <> 'Die Maintenance'
)
SELECT TOP (10)
    co.CustomerOrderId,
    co.LineNumber,
    co.RequestType,
    co.PartNumber,
    co.PartDescription,
    co.RemainingQuantity AS CustomerOrderRemainingQuantity,
    co.DueDate AS CustomerOrderDueDate,
    wo.WorkOrderId,
    wo.RemainingQuantity AS WorkOrderRemainingQuantity,
    wo.DueDate AS WorkOrderDueDate,
    wo.PressOrResourceId,
    wo.PressOrResourceDescription,
    wo.OperationSequence
FROM OpenCustomerOrderLines AS co
INNER JOIN OpenWorkOrders AS wo
    ON wo.PartNumber = co.PartNumber
ORDER BY
    co.DueDate,
    wo.DueDate,
    co.CustomerOrderId,
    wo.WorkOrderId;