SET NOCOUNT ON;
GO

/*
Purpose
Find a customer order and work order you can use for testing against the
Infor Visual database.

What this script looks for
- Open customer order lines with active parts
- Open work orders with active parts
- A simple matched pair by part number

How to use
Run each section in SQL Server Management Studio.
Pick one of the returned order numbers / work orders as the sample record for
your waitlist field tests.

Notes
- The script treats a part as active when it is not detail-only, not inventory locked,
and at least one of fabricated/purchased/stocked is enabled.
- The script treats a customer order line as open when ordered quantity is still
greater than fulfilled quantity.
- The script treats a work order as open when the close date is blank and there is
still remaining quantity.
*/

DECLARE @ActivePartStatus nchar(1) = 'A';
DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

/* =========================================================
   Section 1: Open customer order lines with active parts
   ========================================================= */

;WITH OpenCustomerOrderLines AS
(
    SELECT TOP (25)
        co.ID AS CustomerOrderId,
        co.CUSTOMER_ID AS CustomerId,
        cust.NAME AS CustomerName,
        co.ORDER_DATE AS OrderDate,
        COALESCE(co.PROMISE_DATE, co.DESIRED_SHIP_DATE, col.DESIRED_SHIP_DATE) AS DueDate,
        co.STATUS AS CustomerOrderStatus,
        col.LINE_NO AS LineNumber,
        col.CUST_ORDER_ID AS LineCustomerOrderId,
        col.LINE_STATUS AS LineStatus,
        COALESCE(
            NULLIF(NULLIF(col.ORDER_TYPE, ''), 'Unknown'),
            NULLIF(NULLIF(co.ORDER_TYPE, ''), 'Unknown'),
            NULLIF(NULLIF(col.TYPE, ''), 'Unknown'),
            CASE
                WHEN part.FABRICATED = 'Y' THEN 'Fabricated'
                ELSE 'Fabricated'
            END


) AS RequestType,
        col.PART_ID AS PartNumber,
        part.DESCRIPTION AS PartDescription,
        'Fabricated'

AS PartKind,
        col.ORDER_QTY AS OrderedQuantity,
        col.ALLOCATED_QTY AS AllocatedQuantity,
        col.FULFILLED_QTY AS FulfilledQuantity,
        CASE
            WHEN col.ORDER_QTY - col.FULFILLED_QTY < 0 THEN 0
            ELSE col.ORDER_QTY - col.FULFILLED_QTY
        END AS RemainingQuantity,
        col.DESIRED_SHIP_DATE AS DesiredShipDate,
        col.PROMISE_DATE AS PromiseDate,
        col.SELLING_UM AS SellingUom,
        col.WAREHOUSE_ID AS WarehouseId,
        col.CUSTOMER_PART_ID AS CustomerPartNumber
    FROM CUSTOMER_ORDER AS co
    INNER JOIN CUST_ORDER_LINE AS col
        ON col.CUST_ORDER_ID = co.ID
    INNER JOIN PART AS part
        ON part.ID = col.PART_ID
    LEFT JOIN CUSTOMER AS cust
        ON cust.ID = co.CUSTOMER_ID
    WHERE
        co.ORDER_DATE >= @CutoffDate
        AND COALESCE(NULLIF(part.STATUS, ''), @ActivePartStatus) = @ActivePartStatus
        AND part.DETAIL_ONLY = 'N'
        AND part.INVENTORY_LOCKED = 'N'
        AND part.FABRICATED = 'Y'
        AND col.ORDER_QTY > col.FULFILLED_QTY
    ORDER BY
        COALESCE(co.PROMISE_DATE, co.DESIRED_SHIP_DATE, col.DESIRED_SHIP_DATE),
        col.LINE_NO,
        co.ID
)
SELECT
    CustomerOrderId,
    LineNumber,
    CustomerName,
    RequestType,
    PartNumber,
    PartDescription,
    PartKind,
    OrderedQuantity,
    AllocatedQuantity,
    FulfilledQuantity,
    RemainingQuantity,
    DueDate,
    WarehouseId,
    SellingUom,
    CustomerPartNumber,
    CustomerOrderStatus,
    LineStatus,
    OrderDate
FROM OpenCustomerOrderLines;
GO

/* =========================================================
Section 2: Open work orders with active parts
========================================================= */

DECLARE @ActivePartStatus nchar(1) = 'A';
DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

;WITH OpenWorkOrders AS
(
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
        END


AS RequestType,
        wo.PART_ID AS PartNumber,
        part.DESCRIPTION AS PartDescription,
        'Fabricated'

AS PartKind,
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
        co_match.CustomerOrderId,
        co_match.CustomerName,
        op.SEQUENCE_NO AS OperationSequence,
        op.RESOURCE_ID AS PressOrResourceId,
        sr.DESCRIPTION AS PressOrResourceDescription,
        op.SETUP_HRS AS SetupHours,
        op.RUN_HRS AS RunHours,
        op.STATUS AS OperationStatus,
        op.COMPLETED_QTY AS OperationCompletedQuantity
    FROM WORK_ORDER AS wo
    INNER JOIN PART AS part
        ON part.ID = wo.PART_ID
    OUTER APPLY
    (
        SELECT TOP (1)
            co2.ID AS CustomerOrderId,
            cust2.NAME AS CustomerName
        FROM CUSTOMER_ORDER AS co2
        INNER JOIN CUST_ORDER_LINE AS col2
            ON col2.CUST_ORDER_ID = co2.ID
        LEFT JOIN CUSTOMER AS cust2
            ON cust2.ID = co2.CUSTOMER_ID
        WHERE
            col2.PART_ID = wo.PART_ID
            AND col2.ORDER_QTY > col2.FULFILLED_QTY
            AND co2.STATUS <> 'X'
        ORDER BY
            COALESCE(co2.PROMISE_DATE, co2.DESIRED_SHIP_DATE, col2.DESIRED_SHIP_DATE),
            co2.ID
    ) AS co_match
    OUTER APPLY
    (
        SELECT TOP (1)
            o.SEQUENCE_NO,
            o.RESOURCE_ID,
            o.SETUP_HRS,
            o.RUN_HRS,
            o.STATUS,
            o.COMPLETED_QTY
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
        COALESCE(NULLIF(part.STATUS, ''), @ActivePartStatus) = @ActivePartStatus
        AND part.DETAIL_ONLY = 'N'
        AND part.INVENTORY_LOCKED = 'N'
        AND part.FABRICATED = 'Y'
        AND wo.BASE_ID LIKE 'WO-%'
        AND wo.CLOSE_DATE IS NULL
        AND wo.CREATE_DATE >= @CutoffDate
        AND wo.DESIRED_QTY > wo.FULFILLED_QTY
        AND ISNULL(op.RESOURCE_ID, '') NOT IN ('500-GRP', 'OUTSIDE_SERVICE', 'Materials')
        AND ISNULL(sr.DESCRIPTION, '') <> 'Die Maintenance'
    ORDER BY
        COALESCE(wo.SCHED_FINISH_DATE, wo.DESIRED_RLS_DATE, wo.CREATE_DATE),
        wo.BASE_ID,
        op.SEQUENCE_NO
)
SELECT
    WorkOrderId,
    CustomerOrderId,
    CustomerName,
    PartNumber,
    PartDescription,
    PartKind,
    DesiredQuantity,
    AllocatedQuantity,
    FulfilledQuantity,
    RemainingQuantity,
    WorkOrderStatus,
    CreateDate,
    DesiredReleaseDate,
    DesiredWantDate,
    ScheduledStart,
    ScheduledFinish,
    PlannerId,
    EngineeredBy,
    OperationSequence,
    PressOrResourceId,
    PressOrResourceDescription,
    SetupHours,
    RunHours,
    OperationStatus,
    OperationCompletedQuantity
FROM OpenWorkOrders;
GO

/* =========================================================
Section 3: Suggested CO + WO pair by part number
========================================================= */

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
        COALESCE(NULLIF(part.STATUS, ''), @ActivePartStatus) = @ActivePartStatus
        AND part.DETAIL_ONLY = 'N'
        AND part.INVENTORY_LOCKED = 'N'
        AND part.FABRICATED = 'Y'
        AND wo.BASE_ID LIKE 'WO-%'
        AND wo.CLOSE_DATE IS NULL
        AND wo.CREATE_DATE >= @CutoffDate
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
GO

/* =========================================================
Section 4: Press/resource status table
Active means the resource currently has an open Barcode Labor ticket.
Active rows are shown first; inactive rows follow at the bottom.
========================================================= */

DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

;
WITH
    ResourceLaborStatus AS (
        SELECT
            sr.ID AS PressOrResourceId,
            sr.DESCRIPTION AS PressOrResourceDescription,
            labor_match.EMPLOYEE_ID AS ActiveEmployeeId,
            LTRIM(
                RTRIM(
                    CONCAT(
                        emp.FIRST_NAME,
                        ' ',
                        emp.LAST_NAME
                    )
                )
            ) AS ActiveEmployeeName,
            labor_match.USER_ID AS BarcodeUserId,
            labor_match.WORKORDER_TYPE AS WorkOrderType,
            labor_match.WORKORDER_BASE_ID AS WorkOrderBaseId,
            labor_match.WORKORDER_LOT_ID AS WorkOrderLotId,
            labor_match.WORKORDER_SPLIT_ID AS WorkOrderSplitId,
            labor_match.WORKORDER_SUB_ID AS WorkOrderSubId,
            labor_match.OPERATION_SEQ_NO AS OperationSequence,
            labor_match.CLOCK_IN AS LoginTime,
            COALESCE(
                labor_match.ACT_CLOCK_OUT,
                labor_match.CLOCK_OUT
            ) AS LastActivity,
            CASE
                WHEN labor_match.ROWID IS NOT NULL
                AND COALESCE(
                    labor_match.ACT_CLOCK_OUT,
                    labor_match.CLOCK_OUT
                ) IS NULL THEN 1
                ELSE 0
            END

AS IsActive
        FROM
            SHOP_RESOURCE AS sr OUTER APPLY (
                SELECT
                    TOP (1) lt.ROWID,
                    lt.EMPLOYEE_ID,
                    lt.USER_ID,
                    lt.WORKORDER_TYPE,
                    lt.WORKORDER_BASE_ID,
                    lt.WORKORDER_LOT_ID,
                    lt.WORKORDER_SPLIT_ID,
                    lt.WORKORDER_SUB_ID,
                    lt.OPERATION_SEQ_NO,
                    lt.CLOCK_IN,
                    lt.CLOCK_OUT,
                    lt.ACT_CLOCK_IN,
                    lt.ACT_CLOCK_OUT,
                    lt.TRANSACTION_DATE
                FROM LABOR_TICKET AS lt
                WHERE
                    lt.RESOURCE_ID = sr.ID
                    AND lt.TRANSACTION_DATE >= @CutoffDate
                ORDER BY COALESCE(
                        lt.ACT_CLOCK_IN, lt.CLOCK_IN, lt.TRANSACTION_DATE
                    ) DESC, lt.ROWID DESC
            ) AS labor_match
            LEFT JOIN EMPLOYEE AS emp ON emp.ID = labor_match.EMPLOYEE_ID
        WHERE
            COALESCE(sr.STATUS, 'A') <> 'X'
            AND labor_match.WORKORDER_BASE_ID LIKE '%WO-%'
            AND sr.ID NOT IN(
                '500-GRP',
                'OUTSIDE_SERVICE',
                'Materials'
            )
    )
SELECT
    PressOrResourceId,
    PressOrResourceDescription,
    ActiveEmployeeId,
    ActiveEmployeeName,
    BarcodeUserId,
    WorkOrderType,
    WorkOrderBaseId,
    WorkOrderLotId,
    WorkOrderSplitId,
    WorkOrderSubId,
    OperationSequence,
    LoginTime,
    LastActivity,
    CASE
        WHEN IsActive = 1 THEN 'Active'
        ELSE 'Inactive'
    END AS ResourceStatus
FROM ResourceLaborStatus
ORDER BY
    IsActive DESC,
    LastActivity DESC,
    PressOrResourceDescription,
    PressOrResourceId;