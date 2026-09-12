-- ========================================
-- Module: Waitlist
-- Queue Script: Get Disposition Input
-- Purpose: Return the disposition snapshot for a work center's active setup job + finished product
--          part, feeding MTM_Waitlist.Settings RequestDispositionClassifier.DispositionInput:
--            WorkOrderStatus            WORK_ORDER.STATUS (C/R/U/F/X per ENUM_CODES)
--            OpenWorkOrderQuantity      DESIRED_QTY - RECEIVED_QTY (WIP still open on the order)
--            FinishedGoodsQuantity      PART.QTY_ON_HAND (stock at finished-goods/shipping locations)
--            HasOutsideVendorOperation  an OPERATION row carries VENDOR_ID/SERVICE_ID/SERVICE_PART_ID
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Template: Module_Setup LookupWorkOrder.sql / GetSubordinateParts.sql; Module_Waitlist GetInventoryLocations.sql
-- Parameters:
--   @WorkOrder    - the `WO-######` key (e.g. WO-074011), matched verbatim against WORK_ORDER.BASE_ID
--   @PartNumber   - finished-product part on the order (e.g. 24733431)
--
-- Derivation (file 14 Phase 6, confirmed 2026-09-08 against live data):
--   Infor MT_WIP_INVENTORY is empty in this install, so open order quantity = DESIRED_QTY - RECEIVED_QTY.
--   Outside Service is driven by the vendor/service fields, NOT by a status code.
--   WORK_ORDER.PROD_ORDER_TYPE is all NULL -> not used.
-- Returns zero rows when the work order/part is not found (caller falls back to Unknown).
-- ========================================

SET NOCOUNT ON;

-- Matches the BASE_ID verbatim: the application sends only the canonical `WO-######` key (it accepts
-- lenient operator input and autoformats it first; operator decision 2026-09-12, task T144) and passes
-- that same string to both this live read and the cached
-- copy. The former stripped-base-id fallback resolved bare `M`-family orders the application can no
-- longer ask for and could return a DIFFERENT order than the cache served (T144 collisions).
DECLARE @WorkOrderTrimmed nvarchar(30) = LTRIM(RTRIM(@WorkOrder));

DECLARE @PartNumberNormalized nvarchar(60) = LTRIM(RTRIM(@PartNumber));

SELECT TOP (1)
    LTRIM(RTRIM(wo.STATUS)) AS WorkOrderStatus,
    CAST(
        CASE WHEN wo.DESIRED_QTY > wo.RECEIVED_QTY THEN wo.DESIRED_QTY - wo.RECEIVED_QTY ELSE 0 END
        AS decimal(20, 8)
    ) AS OpenWorkOrderQuantity,
    CAST(COALESCE(part.QTY_ON_HAND, 0) AS decimal(20, 8)) AS FinishedGoodsQuantity,
    CASE WHEN EXISTS
    (
        SELECT 1
        FROM OPERATION AS o
        WHERE o.WORKORDER_TYPE = wo.TYPE
            AND o.WORKORDER_BASE_ID = wo.BASE_ID
            AND o.WORKORDER_LOT_ID = wo.LOT_ID
            AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND o.WORKORDER_SUB_ID = wo.SUB_ID
            AND (LTRIM(RTRIM(ISNULL(o.VENDOR_ID, ''))) <> ''
                 OR LTRIM(RTRIM(ISNULL(o.SERVICE_ID, ''))) <> ''
                 OR LTRIM(RTRIM(ISNULL(o.SERVICE_PART_ID, ''))) <> '')
    ) THEN 1 ELSE 0 END AS HasOutsideVendorOperation
FROM WORK_ORDER AS wo
LEFT JOIN PART AS part
    ON part.ID = wo.PART_ID
WHERE
    wo.BASE_ID = @WorkOrderTrimmed
    AND wo.PART_ID = @PartNumberNormalized
    AND wo.PART_ID IS NOT NULL;
