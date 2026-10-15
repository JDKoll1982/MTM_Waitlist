-- ========================================
-- Module: Waitlist
-- Queue Script: Get Disposition Input
-- Purpose: Return the disposition snapshot for a work center's active setup job + finished product
--          part, feeding MTM_Waitlist.Settings RequestDispositionClassifier.DispositionInput:
--            WorkOrderStatus            WORK_ORDER.STATUS (C/R/U/F/X per ENUM_CODES)
--            OpenWorkOrderQuantity      DESIRED_QTY - RECEIVED_QTY (WIP still open on the order)
--            FinishedGoodsQuantity      SUM of PART_LOCATION.QTY at the part's finished-goods locations, so a
--                                       part with stock anywhere else does not read as Finished Goods (FR-090)
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
--   FinishedGoodsQuantity is the part's stock AT A FINISHED-GOODS LOCATION, not PART.QTY_ON_HAND. FR-090 records
--   the defect that PART.QTY_ON_HAND stood here: it is the part's total on-hand across every location and carries
--   no location filter at all, so any stock anywhere plus a closed order with nothing open read as Finished
--   Goods. The classifier's documented rule ("on-hand > 0 at a finished-goods / shipping location") is unchanged;
--   only the value handed to it is corrected (research R8).
--   Finished-goods classification mirrors the taxonomy already documented for the WIP side in
--   Database/MTMWipApp/Queues/Module_Waitlist/Queues/GetWipFloorQuantities.sql and already used by
--   GetPartWarehouseQuantities.sql: a location is finished goods when its own DESCRIPTION says so, when its
--   WAREHOUSE.DESCRIPTION says so, or when its code is 'FG' / 'DC-FG'. LOCATION is keyed by (WAREHOUSE_ID, ID),
--   so the join carries both. These codes are NOT YET CONFIRMED for this installation: task T067's live
--   validation (Database/Validation/disposition_input_finished_goods/validate.sql) is what confirms them, and no
--   code is invented before it does.
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
    CAST(COALESCE(
    (
        SELECT SUM(COALESCE(pl.QTY, 0))
        FROM PART_LOCATION AS pl
        LEFT JOIN WAREHOUSE AS wh
            ON wh.ID = pl.WAREHOUSE_ID
        LEFT JOIN LOCATION AS loc
            ON loc.WAREHOUSE_ID = pl.WAREHOUSE_ID
            AND loc.ID = pl.LOCATION_ID
        WHERE pl.PART_ID = wo.PART_ID
            AND (ISNULL(loc.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                 OR ISNULL(wh.DESCRIPTION, '') LIKE '%FINISHED GOODS%'
                 OR ISNULL(loc.ID, '') IN ('FG', 'DC-FG'))
    ), 0) AS decimal(20, 8)) AS FinishedGoodsQuantity,
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
WHERE
    wo.BASE_ID = @WorkOrderTrimmed
    AND wo.PART_ID = @PartNumberNormalized
    AND wo.PART_ID IS NOT NULL;
