-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Work order lookup (shape 1 of 5: work_order_lookup)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 1 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here, not supplied by the caller)
--
-- Scope (operator decision, 2026-09-10 — tasks.md Phase 5 note / task T113, narrowed 2026-09-11 by
-- task T144): the ADDRESSABLE open work orders. Every open order is in scope by status
-- (WORK_ORDER.STATUS in ('R' Released, 'U' Unreleased, 'F' Firmed); closed 'C' and cancelled 'X' are
-- never cached), but only those the application can ask for AND the live read can answer are keyed.
--
-- Key domain (operator decision 2026-09-12, task T144 — narrowed to the `WO-######` form ONLY):
--   The application now accepts only `WO-` + exactly six digits and keys the cache by that exact
--   normalized string (WorkOrderValidationService; sp_visual_work_order_lookup_get matches
--   `normalized_work_order = p_normalized_work_order`), so this script emits a key only where BASE_ID
--   is literally `WO-` + 6 digits and uses the BASE_ID itself as the key. The live per-key read
--   (LookupWorkOrder.sql) matches that same verbatim `BASE_ID`, so cached and live answers are keyed
--   by exactly the same string.
--   Everything else is deliberately EXCLUDED because the application can no longer ask for it:
--     * bare 6-digit numeric BASE_IDs (the `M` family) - the former rule accepted the bare form and
--       padded it to `WO-0xxxxx`, a key the live read resolved only through its stripped-form match;
--       for ids that also exist as a `WO-0xxxxx` order that answered with a DIFFERENT order (the T144
--       cross-order collisions). Not reachable now that the rule requires the prefix.
--     * 5-digit numeric ids - the application would have padded them to a key nothing answered.
--     * the `Q`, non-numeric-`M` and 7+-digit families - never expressible by the rule.
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
        -- Addressable work orders only (operator decision 2026-09-12, task T144). The mirror must hold
        -- exactly the keys the application can ask for AND the live per-key read (LookupWorkOrder.sql)
        -- resolves, or the fallback is not transparent (FR-002/FR-004): a key the live read cannot
        -- answer would let an outage serve an order that a healthy Infor Visual never returns for that
        -- input. The application now accepts only the `WO-######` form, so BASE_ID must literally be
        -- `WO-` + 6 digits. Full rationale in the header.
        AND LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
        AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
        AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%'
)
SELECT DISTINCT
    LTRIM(RTRIM(ow.BaseId)) AS NormalizedWorkOrder,
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
