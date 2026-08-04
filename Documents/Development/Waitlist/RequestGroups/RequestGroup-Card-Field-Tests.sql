SET NOCOUNT ON;
GO

/*
Purpose
Test the fields recommended for the waitlist group cards.

How to use
Replace the sample IDs in each section with a real order, work order, or packlist value
from the Infor Visual database, then run the section you want to check in SQL Server
Management Studio.

What this script shows
- Request type
- Order number
- Customer name
- Part number and short description
- Press or resource
- Remaining time
- Quantity still open
- Current status
*/

/* =========================================================
Section 1: Customer order line card check
Best for coil, pickup NCM, and similar request types.
========================================================= */

DECLARE @CustomerOrderId nvarchar(15) = 'REPLACE_ME';

SELECT
    'Customer order line' AS RequestFamily,
    COALESCE(NULLIF(col.ORDER_TYPE, ''), NULLIF(co.TYPE, ''), NULLIF(col.TYPE, ''), 'Unknown') AS RequestType,
    co.ID AS OrderNumber,
    cust.NAME AS CustomerName,
    col.CUST_ORDER_ID AS CustomerOrderId,
    col.LINE_NO AS LineNumber,
    col.PART_ID AS PartNumber,
    part.DESCRIPTION AS PartDescription,
    wo.RESOURCE_ID AS PressOrResourceId,
    sr.DESCRIPTION AS PressOrResourceDescription,
    COALESCE(co.PROMISE_DATE, col.DESIRED_SHIP_DATE) AS TargetDate,
    DATEDIFF(DAY, GETDATE(), COALESCE(co.PROMISE_DATE, col.DESIRED_SHIP_DATE)) AS RemainingTimeDays,
    CASE
        WHEN COALESCE(co.PROMISE_DATE, col.DESIRED_SHIP_DATE) IS NULL THEN 'No due date'
        ELSE CONCAT(DATEDIFF(DAY, GETDATE(), COALESCE(co.PROMISE_DATE, col.DESIRED_SHIP_DATE)), ' days')
    END

AS RemainingTimeText,
col.ORDER_QTY AS OrderedQuantity,
col.ALLOCATED_QTY AS AllocatedQuantity,
col.FULFILLED_QTY AS FulfilledQuantity,
CASE
    WHEN col.ORDER_QTY - col.FULFILLED_QTY < 0 THEN 0
    ELSE col.ORDER_QTY - col.FULFILLED_QTY
END AS RemainingQuantity,
col.LINE_STATUS AS LineStatus,
co.STATUS AS OrderStatus,
wo.TYPE AS WorkOrderType,
wo.BASE_ID AS WorkOrderBaseId,
wo.LOT_ID AS WorkOrderLotId,
wo.SPLIT_ID AS WorkOrderSplitId,
wo.SUB_ID AS WorkOrderSubId,
wo.STATUS AS WorkOrderStatus,
wo.SCHED_START_DATE AS WorkOrderScheduledStart,
wo.SCHED_FINISH_DATE AS WorkOrderScheduledFinish
FROM
    CUST_ORDER_LINE AS col
    LEFT JOIN CUSTOMER_ORDER AS co ON co.ID = col.CUST_ORDER_ID
    LEFT JOIN CUSTOMER AS cust ON cust.ID = co.CUSTOMER_ID
    LEFT JOIN PART AS part ON part.ID = col.PART_ID OUTER APPLY (
        SELECT
            TOP (1) w.TYPE,
            w.BASE_ID,
            w.LOT_ID,
            w.SPLIT_ID,
            w.SUB_ID,
            w.STATUS,
            w.SCHED_START_DATE,
            w.SCHED_FINISH_DATE,
            o.RESOURCE_ID,
            o.SEQUENCE_NO,
            o.SCHED_FINISH_DATE AS OperationScheduledFinish
        FROM
            WORK_ORDER AS w
            LEFT JOIN OPERATION AS o ON o.WORKORDER_TYPE = w.TYPE
            AND o.WORKORDER_BASE_ID = w.BASE_ID
            AND o.WORKORDER_LOT_ID = w.LOT_ID
            AND o.WORKORDER_SPLIT_ID = w.SPLIT_ID
            AND o.WORKORDER_SUB_ID = w.SUB_ID
        WHERE
            w.WBS_CUST_ORDER_ID = co.ID
            OR w.PART_ID = col.PART_ID
        ORDER BY w.SCHED_FINISH_DATE, o.SEQUENCE_NO
    ) AS wo
    LEFT JOIN SHOP_RESOURCE AS sr ON sr.ID = wo.RESOURCE_ID
WHERE
    col.CUST_ORDER_ID = @CustomerOrderId
ORDER BY col.LINE_NO;
GO

/* =========================================================
Section 2: Finished goods pickup card check
This is the best place to verify shipper fields.
========================================================= */

DECLARE @FinishedGoodsCustomerOrderId nvarchar(15) = 'REPLACE_ME';

SELECT TOP (20)
    'Pickup FG' AS RequestFamily,
    COALESCE(NULLIF(co.ORDER_TYPE, ''), NULLIF(col.ORDER_TYPE, ''), 'Pickup FG') AS RequestType,
    co.ID AS OrderNumber,
    cust.NAME AS CustomerName,
    col.PART_ID AS PartNumber,
    part.DESCRIPTION AS PartDescription,
    sh.PACKLIST_ID AS PacklistId,
    sh.SHIP_VIA AS ShipVia,
    sh.EXPECTED_DEL_DATE AS ExpectedDeliveryDate,
    DATEDIFF(DAY, GETDATE(), COALESCE(sh.EXPECTED_DEL_DATE, co.PROMISE_DATE, col.DESIRED_SHIP_DATE)) AS RemainingTimeDays,
    CONCAT(DATEDIFF(DAY, GETDATE(), COALESCE(sh.EXPECTED_DEL_DATE, co.PROMISE_DATE, col.DESIRED_SHIP_DATE)), ' days') AS RemainingTimeText,
    col.ORDER_QTY AS OrderedQuantity,
    col.ALLOCATED_QTY AS AllocatedQuantity,
    col.FULFILLED_QTY AS FulfilledQuantity,
    CASE
        WHEN col.ORDER_QTY - col.FULFILLED_QTY < 0 THEN 0
        ELSE col.ORDER_QTY - col.FULFILLED_QTY
    END

AS RemainingQuantity,
col.LINE_STATUS AS LineStatus,
co.STATUS AS OrderStatus
FROM
    CUSTOMER_ORDER AS co
    INNER JOIN CUST_ORDER_LINE AS col ON col.CUST_ORDER_ID = co.ID
    LEFT JOIN CUSTOMER AS cust ON cust.ID = co.CUSTOMER_ID
    LEFT JOIN PART AS part ON part.ID = col.PART_ID OUTER APPLY (
        SELECT TOP (1) s.PACKLIST_ID, s.SHIP_VIA, s.EXPECTED_DEL_DATE, s.STATUS, s.SHIPPED_DATE
        FROM SHIPPER AS s
        WHERE
            s.CUST_ORDER_ID = co.ID
        ORDER BY s.CREATE_DATE DESC, s.SHIPPED_DATE DESC
    ) AS sh
WHERE
    co.ID = @FinishedGoodsCustomerOrderId;
GO

/* =========================================================
Section 3: Work-order driven card check
Best for WIP, outside service, and scrap style requests.
========================================================= */

DECLARE @WorkOrderType nchar(1) = 'R';
DECLARE @WorkOrderBaseId nvarchar(30) = 'REPLACE_ME';
DECLARE @WorkOrderLotId nvarchar(3) = '   ';
DECLARE @WorkOrderSplitId nvarchar(3) = '   ';
DECLARE @WorkOrderSubId nvarchar(3) = '   ';

SELECT
    'Work order' AS RequestFamily,
    COALESCE(NULLIF(wo.STATUS, ''), 'Unknown') AS RequestType,
    CONCAT(wo.TYPE, '-', wo.BASE_ID, '-', wo.LOT_ID, '-', wo.SPLIT_ID, '-', wo.SUB_ID) AS WorkOrderId,
    wo.PART_ID AS PartNumber,
    part.DESCRIPTION AS PartDescription,
    wo.DESIRED_QTY AS DesiredQuantity,
    wo.ALLOCATED_QTY AS AllocatedQuantity,
    wo.FULFILLED_QTY AS FulfilledQuantity,
    CASE
        WHEN wo.DESIRED_QTY - wo.FULFILLED_QTY < 0 THEN 0
        ELSE wo.DESIRED_QTY - wo.FULFILLED_QTY
    END

AS RemainingQuantity,
wo.STATUS AS WorkOrderStatus,
wo.DBR_PRIORITY AS Priority,
wo.DBR_CODE AS DispatchRuleCode,
wo.SCHED_START_DATE AS ScheduledStart,
wo.SCHED_FINISH_DATE AS ScheduledFinish,
op.SEQUENCE_NO AS OperationSequence,
op.RESOURCE_ID AS PressOrResourceId,
sr.DESCRIPTION AS PressOrResourceDescription,
op.SETUP_HRS AS SetupHours,
op.RUN_HRS AS RunHours,
op.STATUS AS OperationStatus,
op.COMPLETED_QTY AS OperationCompletedQuantity,
wb.MATERIAL_AMOUNT AS WipMaterialAmount,
wb.LABOR_AMOUNT AS WipLaborAmount,
wb.BURDEN_AMOUNT AS WipBurdenAmount,
wb.SERVICE_AMOUNT AS WipServiceAmount
FROM
    WORK_ORDER AS wo
    LEFT JOIN PART AS part ON part.ID = wo.PART_ID OUTER APPLY (
        SELECT TOP (1) o.SEQUENCE_NO, o.RESOURCE_ID, o.SETUP_HRS, o.RUN_HRS, o.STATUS, o.COMPLETED_QTY, o.SCHED_START_DATE, o.SCHED_FINISH_DATE
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
    LEFT JOIN WIP_BALANCE AS wb ON wb.WORKORDER_TYPE = wo.TYPE
    AND wb.WORKORDER_BASE_ID = wo.BASE_ID
    AND wb.WORKORDER_LOT_ID = wo.LOT_ID
    AND wb.WORKORDER_SPLIT_ID = wo.SPLIT_ID
    AND wb.WORKORDER_SUB_ID = wo.SUB_ID
WHERE
    wo.TYPE = @WorkOrderType
    AND wo.BASE_ID = @WorkOrderBaseId
    AND wo.LOT_ID = @WorkOrderLotId
    AND wo.SPLIT_ID = @WorkOrderSplitId
    AND wo.SUB_ID = @WorkOrderSubId;
GO