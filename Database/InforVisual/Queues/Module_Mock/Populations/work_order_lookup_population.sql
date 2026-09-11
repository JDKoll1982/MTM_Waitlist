-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Work order lookup (shape 1 of 5: work_order_lookup)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 1 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope (operator decision, 2026-09-10 — tasks.md Phase 5 note / task T113):
--   Every OPEN work order: WORK_ORDER.STATUS in ('R' Released, 'U' Unreleased, 'F' Firmed).
--   Closed ('C') and cancelled ('X') orders are never cached. On the live data of 2026-09-08 that
--   is 42,138 + 680 + 0 = 42,818 orders out of 72,500.
--
-- Key domain:
--   The application only ever queries a work order it accepted from the operator, and
--   WorkOrderValidationService accepts exactly `^(?:WO-)?(\d{5,6})$` and normalizes it to
--   `WO-` + the 6-digit, zero-padded base id. The mirror is keyed by that exact normalized string
--   (sp_visual_work_order_lookup_get matches `normalized_work_order = p_normalized_work_order`), so
--   this script emits only base ids that the application can actually ask for and pads them the same
--   way. A base id outside that domain could never be requested, and truncating a longer id would
--   collide with a different order — so it is excluded deliberately rather than silently mangled.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_work_order_lookup_refresh):
--   NormalizedWorkOrder (input key), PartNumber, Description, WorkCenter.
--   LookupWorkOrder.sql is the per-key live read this projection is copied verbatim from; the
--   payload source additionally validates the returned column set against the catalog at runtime so
--   drift is reported as a schema mismatch instead of loading a wrong-shaped snapshot.
-- ========================================

SET NOCOUNT ON;

WITH open_work_orders AS
(
    SELECT
        wo.TYPE AS WorkOrderType,
        wo.BASE_ID AS BaseId,
        wo.LOT_ID AS LotId,
        wo.SPLIT_ID AS SplitId,
        wo.SUB_ID AS SubId,
        wo.PART_ID AS PartId
    FROM WORK_ORDER AS wo
    WHERE
        wo.STATUS IN ('R', 'U', 'F')
        AND wo.BASE_ID IS NOT NULL
        AND wo.PART_ID IS NOT NULL
        AND LEN(LTRIM(RTRIM(wo.BASE_ID))) BETWEEN 5 AND 6
        AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%'
)
SELECT DISTINCT
    'WO-' + RIGHT('000000' + LTRIM(RTRIM(ow.BaseId)), 6) AS NormalizedWorkOrder,
    ow.PartId AS PartNumber,
    COALESCE(part.DESCRIPTION, '') AS Description,
    COALESCE(op.RESOURCE_ID, '') AS WorkCenter
FROM open_work_orders AS ow
LEFT JOIN PART AS part
    ON part.ID = ow.PartId
OUTER APPLY
(
    SELECT TOP (1)
        o.RESOURCE_ID
    FROM OPERATION AS o
    WHERE
        o.WORKORDER_TYPE = ow.WorkOrderType
        AND o.WORKORDER_BASE_ID = ow.BaseId
        AND o.WORKORDER_LOT_ID = ow.LotId
        AND o.WORKORDER_SPLIT_ID = ow.SplitId
        AND o.WORKORDER_SUB_ID = ow.SubId
    ORDER BY o.SEQUENCE_NO
) AS op
ORDER BY
    NormalizedWorkOrder,
    PartNumber;
