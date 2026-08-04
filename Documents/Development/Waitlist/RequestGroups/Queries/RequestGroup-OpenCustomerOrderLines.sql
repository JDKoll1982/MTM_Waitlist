SET NOCOUNT ON;

DECLARE @ActivePartStatus nchar(1) = 'A';
DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

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
        'Fabricated'
    ) AS RequestType,
    col.PART_ID AS PartNumber,
    part.DESCRIPTION AS PartDescription,
    'Fabricated' AS PartKind,
    col.ORDER_QTY AS OrderedQuantity,
    col.ALLOCATED_QTY AS AllocatedQuantity,
    col.FULFILLED_QTY AS FulfilledQuantity,
    CASE
        WHEN col.ORDER_QTY - col.FULFILLED_QTY < 0 THEN 0
        ELSE col.ORDER_QTY - col.FULFILLED_QTY
    END

AS RemainingQuantity,
col.DESIRED_SHIP_DATE AS DesiredShipDate,
col.PROMISE_DATE AS PromiseDate,
col.SELLING_UM AS SellingUom,
col.WAREHOUSE_ID AS WarehouseId,
col.CUSTOMER_PART_ID AS CustomerPartNumber
FROM
    CUSTOMER_ORDER AS co
    INNER JOIN CUST_ORDER_LINE AS col ON col.CUST_ORDER_ID = co.ID
    INNER JOIN PART AS part ON part.ID = col.PART_ID
    LEFT JOIN CUSTOMER AS cust ON cust.ID = co.CUSTOMER_ID
WHERE
    co.ORDER_DATE >= @CutoffDate
    AND COALESCE(
        NULLIF(part.STATUS, ''),
        @ActivePartStatus
    ) = @ActivePartStatus
    AND part.DETAIL_ONLY = 'N'
    AND part.INVENTORY_LOCKED = 'N'
    AND part.FABRICATED = 'Y'
    AND col.ORDER_QTY > col.FULFILLED_QTY
ORDER BY COALESCE(
        co.PROMISE_DATE, co.DESIRED_SHIP_DATE, col.DESIRED_SHIP_DATE
    ), col.LINE_NO, co.ID;