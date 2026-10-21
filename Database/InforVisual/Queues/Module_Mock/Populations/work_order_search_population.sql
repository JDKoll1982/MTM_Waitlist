-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Work order search (shape 8 of 8: work_order_search)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 8 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope
-- -----
-- The same ADDRESSABLE open work orders the per-key read searches: STATUS in ('R','U','F') with a
-- canonical `WO-` + six-digit BASE_ID. That is the whole domain the search can return, so the mirror
-- holds every answer the live read can give (FR-002/FR-004 transparency). It is a few hundred rows.
--
-- Driver keys
-- -----------
-- The shape's declared inputs are the three things an operator searches BY, not a row key, so the
-- snapshot projects each row's own values into them: WorkOrderText is the work order, PartFilter is
-- its part number, and CustomerFilter is its customer's name. They are stored beside the output
-- columns exactly as `inventory_locations` stores its PartNumber input beside its PartNumber output,
-- and the read procedure filters on the OUTPUT columns.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_work_order_search_refresh):
--   WorkOrderText, PartFilter, CustomerFilter,
--   WorkOrder, PartNumber, PartDescription, CustomerName, WorkCenter, Status, WantDate.
-- GetWorkOrderSearch.sql is the per-key live read this projection is copied verbatim from.
-- ========================================

SET NOCOUNT ON;

SELECT
    LTRIM(RTRIM(wo.BASE_ID)) AS WorkOrderText,
    wo.PART_ID AS PartFilter,
    COALESCE(cust.CUST_NAME, '') AS CustomerFilter,
    LTRIM(RTRIM(wo.BASE_ID)) AS WorkOrder,
    wo.PART_ID AS PartNumber,
    COALESCE(part.DESCRIPTION, '') AS PartDescription,
    COALESCE(cust.CUST_NAME, '') AS CustomerName,
    COALESCE(op.RESOURCE_ID, '') AS WorkCenter,
    LTRIM(RTRIM(wo.STATUS)) AS Status,
    CAST(wo.DESIRED_WANT_DATE AS date) AS WantDate
FROM WORK_ORDER AS wo
LEFT JOIN PART AS part
    ON part.ID = wo.PART_ID
OUTER APPLY
(
    SELECT TOP (1)
        a.CUST_NAME
    FROM CUST_ORDER_ALLOC AS a
    WHERE
        a.SUPPLY_TYPE = 'WO'
        AND a.BASE_ID = wo.BASE_ID
        AND NULLIF(LTRIM(RTRIM(a.CUST_NAME)), '') IS NOT NULL
    ORDER BY
        a.WANT_DATE DESC,
        a.CUST_NAME
) AS cust
OUTER APPLY
(
    SELECT TOP (1)
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
WHERE
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    AND LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
    AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
    AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%'
ORDER BY
    WorkOrder;
