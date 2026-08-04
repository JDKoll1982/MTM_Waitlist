SET NOCOUNT ON;
GO

/*
Purpose
Test the fields recommended for the waitlist details page.

How to use
Replace the sample IDs with a real order or work order from the Infor Visual database.
Run the section you want to check in SQL Server Management Studio.

What this script shows
- Full request summary
- Customer and contact details
- Part and quantity details
- Work order and operation details
- Schedule, shipment, and WIP details
- A small placeholder block showing what future template sections can look like
*/

/* =========================================================
Section 1: Full customer order detail view
========================================================= */

DECLARE @CustomerOrderId nvarchar(15) = 'REPLACE_ME';

SELECT
    co.ID AS OrderNumber,
    co.CUSTOMER_ID AS CustomerId,
    cust.NAME AS CustomerName,
    COALESCE(NULLIF(co.CONTACT_FIRST_NAME, ''), '') +
        CASE WHEN NULLIF(co.CONTACT_LAST_NAME, '') IS NULL THEN '' ELSE ' ' + co.CONTACT_LAST_NAME END

AS CustomerContactName,
co.CONTACT_PHONE AS CustomerContactPhone,
co.CONTACT_EMAIL AS CustomerContactEmail,
co.CUSTOMER_PO_REF AS CustomerPoReference,
co.ORDER_DATE AS OrderDate,
co.DESIRED_SHIP_DATE AS DesiredShipDate,
co.PROMISE_DATE AS PromiseDate,
co.STATUS AS OrderStatus,
co.BACK_ORDER AS BackOrderFlag,
co.SHIP_VIA AS ShipVia,
co.FREE_ON_BOARD AS FreeOnBoard,
co.WAREHOUSE_ID AS WarehouseId,
col.LINE_NO AS LineNumber,
col.CUST_ORDER_ID AS LineOrderNumber,
col.PART_ID AS PartNumber,
part.DESCRIPTION AS PartDescription,
col.CUSTOMER_PART_ID AS CustomerPartNumber,
col.ORDER_QTY AS OrderedQuantity,
col.ALLOCATED_QTY AS AllocatedQuantity,
col.FULFILLED_QTY AS FulfilledQuantity,
CASE
    WHEN col.ORDER_QTY - col.FULFILLED_QTY < 0 THEN 0
    ELSE col.ORDER_QTY - col.FULFILLED_QTY
END AS RemainingQuantity,
col.DESIRED_SHIP_DATE AS LineDesiredShipDate,
col.PROMISE_DATE AS LinePromiseDate,
col.LINE_STATUS AS LineStatus,
col.SELLING_UM AS SellingUom,
col.PIECE_COUNT AS PieceCount,
col.LENGTH AS PartLength,
col.WIDTH AS PartWidth,
col.HEIGHT AS PartHeight,
col.DIMENSIONS_UM AS DimensionsUom,
wo.TYPE AS WorkOrderType,
wo.BASE_ID AS WorkOrderBaseId,
wo.LOT_ID AS WorkOrderLotId,
wo.SPLIT_ID AS WorkOrderSplitId,
wo.SUB_ID AS WorkOrderSubId,
wo.DESIRED_QTY AS WorkOrderDesiredQuantity,
wo.RECEIVED_QTY AS WorkOrderReceivedQuantity,
wo.ALLOCATED_QTY AS WorkOrderAllocatedQuantity,
wo.FULFILLED_QTY AS WorkOrderFulfilledQuantity,
wo.STATUS AS WorkOrderStatus,
wo.DESIRED_RLS_DATE AS WorkOrderDesiredReleaseDate,
wo.SCHED_START_DATE AS WorkOrderScheduledStart,
wo.SCHED_FINISH_DATE AS WorkOrderScheduledFinish,
op.SEQUENCE_NO AS OperationSequence,
op.RESOURCE_ID AS PressOrResourceId,
sr.DESCRIPTION AS PressOrResourceDescription,
op.SETUP_HRS AS SetupHours,
op.RUN_HRS AS RunHours,
op.COMPLETED_QTY AS OperationCompletedQuantity,
op.STATUS AS OperationStatus,
sh.PACKLIST_ID AS PacklistId,
sh.SHIPPED_DATE AS ShippedDate,
sh.EXPECTED_DEL_DATE AS ExpectedDeliveryDate,
wb.MATERIAL_AMOUNT AS WipMaterialAmount,
wb.LABOR_AMOUNT AS WipLaborAmount,
wb.BURDEN_AMOUNT AS WipBurdenAmount,
wb.SERVICE_AMOUNT AS WipServiceAmount
FROM
    CUSTOMER_ORDER AS co
    LEFT JOIN CUSTOMER AS cust ON cust.ID = co.CUSTOMER_ID
    LEFT JOIN CUST_ORDER_LINE AS col ON col.CUST_ORDER_ID = co.ID
    LEFT JOIN PART AS part ON part.ID = col.PART_ID OUTER APPLY (
        SELECT TOP (1) w.TYPE, w.BASE_ID, w.LOT_ID, w.SPLIT_ID, w.SUB_ID, w.DESIRED_QTY, w.RECEIVED_QTY, w.ALLOCATED_QTY, w.FULFILLED_QTY, w.STATUS, w.DESIRED_RLS_DATE, w.SCHED_START_DATE, w.SCHED_FINISH_DATE
        FROM WORK_ORDER AS w
        WHERE
            w.WBS_CUST_ORDER_ID = co.ID
        ORDER BY w.SCHED_FINISH_DATE, w.DESIRED_RLS_DATE
    ) AS wo OUTER APPLY (
        SELECT TOP (1) o.SEQUENCE_NO, o.RESOURCE_ID, o.SETUP_HRS, o.RUN_HRS, o.COMPLETED_QTY, o.STATUS
        FROM OPERATION AS o
        WHERE
            o.WORKORDER_TYPE = wo.TYPE
            AND o.WORKORDER_BASE_ID = wo.BASE_ID
            AND o.WORKORDER_LOT_ID = wo.LOT_ID
            AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND o.WORKORDER_SUB_ID = wo.SUB_ID
        ORDER BY o.SEQUENCE_NO
    ) AS op
    LEFT JOIN SHOP_RESOURCE AS sr ON sr.ID = op.RESOURCE_ID OUTER APPLY (
        SELECT TOP (1) s.PACKLIST_ID, s.SHIPPED_DATE, s.EXPECTED_DEL_DATE, s.SHIP_VIA, s.STATUS
        FROM SHIPPER AS s
        WHERE
            s.CUST_ORDER_ID = co.ID
        ORDER BY s.CREATE_DATE DESC, s.SHIPPED_DATE DESC
    ) AS sh
    LEFT JOIN WIP_BALANCE AS wb ON wb.WORKORDER_TYPE = wo.TYPE
    AND wb.WORKORDER_BASE_ID = wo.BASE_ID
    AND wb.WORKORDER_LOT_ID = wo.LOT_ID
    AND wb.WORKORDER_SPLIT_ID = wo.SPLIT_ID
    AND wb.WORKORDER_SUB_ID = wo.SUB_ID
WHERE
    co.ID = @CustomerOrderId;
GO

/* =========================================================
Section 2: Work-order centered detail view
========================================================= */

DECLARE @WorkOrderType nchar(1) = 'R';
DECLARE @WorkOrderBaseId nvarchar(30) = 'REPLACE_ME';
DECLARE @WorkOrderLotId nvarchar(3) = '   ';
DECLARE @WorkOrderSplitId nvarchar(3) = '   ';
DECLARE @WorkOrderSubId nvarchar(3) = '   ';

SELECT
    wo.TYPE AS WorkOrderType,
    wo.BASE_ID AS WorkOrderBaseId,
    wo.LOT_ID AS WorkOrderLotId,
    wo.SPLIT_ID AS WorkOrderSplitId,
    wo.SUB_ID AS WorkOrderSubId,
    wo.PART_ID AS PartNumber,
    part.DESCRIPTION AS PartDescription,
    wo.DESIRED_QTY AS DesiredQuantity,
    wo.RECEIVED_QTY AS ReceivedQuantity,
    wo.ALLOCATED_QTY AS AllocatedQuantity,
    wo.FULFILLED_QTY AS FulfilledQuantity,
    CASE
        WHEN wo.DESIRED_QTY - wo.FULFILLED_QTY < 0 THEN 0
        ELSE wo.DESIRED_QTY - wo.FULFILLED_QTY
    END

AS RemainingQuantity,
wo.CREATE_DATE AS CreateDate,
wo.DESIRED_RLS_DATE AS DesiredReleaseDate,
wo.DESIRED_WANT_DATE AS DesiredWantDate,
wo.CLOSE_DATE AS CloseDate,
wo.COSTED_DATE AS CostedDate,
wo.STATUS AS WorkOrderStatus,
wo.SCHEDULE_GROUP_ID AS ScheduleGroupId,
wo.SCHED_START_DATE AS ScheduledStart,
wo.SCHED_FINISH_DATE AS ScheduledFinish,
wo.COULD_FINISH_DATE AS CouldFinishDate,
wo.PLANNER_ID AS PlannerId,
wo.ENGINEERED_BY AS EngineeredBy,
wo.ENGINEERED_DATE AS EngineeredDate,
op.SEQUENCE_NO AS OperationSequence,
op.RESOURCE_ID AS PressOrResourceId,
sr.DESCRIPTION AS PressOrResourceDescription,
op.SETUP_HRS AS SetupHours,
op.RUN_HRS AS RunHours,
op.ACT_SETUP_HRS AS ActualSetupHours,
op.ACT_RUN_HRS AS ActualRunHours,
op.SCHED_START_DATE AS OperationScheduledStart,
op.SCHED_FINISH_DATE AS OperationScheduledFinish,
op.COMPLETED_QTY AS OperationCompletedQuantity,
op.DISPATCHED_QTY AS OperationDispatchedQuantity,
op.STATUS AS OperationStatus,
req.PART_ID AS RequirementPartId,
req.CALC_QTY AS RequirementCalculatedQuantity,
req.ISSUED_QTY AS RequirementIssuedQuantity,
req.ALLOCATED_QTY AS RequirementAllocatedQuantity,
req.FULFILLED_QTY AS RequirementFulfilledQuantity,
req.REQUIRED_DATE AS RequirementRequiredDate,
wb.MATERIAL_AMOUNT AS WipMaterialAmount,
wb.LABOR_AMOUNT AS WipLaborAmount,
wb.BURDEN_AMOUNT AS WipBurdenAmount,
wb.SERVICE_AMOUNT AS WipServiceAmount
FROM
    WORK_ORDER AS wo
    LEFT JOIN PART AS part ON part.ID = wo.PART_ID OUTER APPLY (
        SELECT TOP (1) o.SEQUENCE_NO, o.RESOURCE_ID, o.SETUP_HRS, o.RUN_HRS, o.ACT_SETUP_HRS, o.ACT_RUN_HRS, o.SCHED_START_DATE, o.SCHED_FINISH_DATE, o.COMPLETED_QTY, o.DISPATCHED_QTY, o.STATUS
        FROM OPERATION AS o
        WHERE
            o.WORKORDER_TYPE = wo.TYPE
            AND o.WORKORDER_BASE_ID = wo.BASE_ID
            AND o.WORKORDER_LOT_ID = wo.LOT_ID
            AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND o.WORKORDER_SUB_ID = wo.SUB_ID
        ORDER BY o.SEQUENCE_NO
    ) AS op
    LEFT JOIN SHOP_RESOURCE AS sr ON sr.ID = op.RESOURCE_ID OUTER APPLY (
        SELECT TOP (1) r.PART_ID, r.CALC_QTY, r.ISSUED_QTY, r.ALLOCATED_QTY, r.FULFILLED_QTY, r.REQUIRED_DATE
        FROM REQUIREMENT AS r
        WHERE
            r.WORKORDER_TYPE = wo.TYPE
            AND r.WORKORDER_BASE_ID = wo.BASE_ID
            AND r.WORKORDER_LOT_ID = wo.LOT_ID
            AND r.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND r.WORKORDER_SUB_ID = wo.SUB_ID
        ORDER BY r.REQUIRED_DATE, r.OPERATION_SEQ_NO, r.PIECE_NO
    ) AS req
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

/* =========================================================
Section 3: Simple future-template placeholder output
This is not live database data. It is only a shape preview.
========================================================= */

SELECT
    'Scheduling preview' AS SectionName,
    'Queued at' AS FieldName,
    '08:15' AS FieldValue
UNION ALL
SELECT 'Scheduling preview', 'Target completion', '08:45'
UNION ALL
SELECT 'Material preview', 'Material code', 'MAT-00124'
UNION ALL
SELECT 'Workflow preview', 'Next step', 'Forklift dispatch';
GO