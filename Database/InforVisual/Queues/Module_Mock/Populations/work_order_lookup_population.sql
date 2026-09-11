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
-- Key domain (T144 — corrected):
--   The application only ever queries a work order it accepted from the operator, and
--   WorkOrderValidationService accepts exactly `^(?:WO-)?(\d{5,6})$` and normalizes it to
--   `WO-` + the 6-digit, zero-padded base id. The mirror is keyed by that exact normalized string
--   (sp_visual_work_order_lookup_get matches `normalized_work_order = p_normalized_work_order`), so
--   this script emits keys in that same form. Two source shapes produce such a key, and both are
--   matched by the live read:
--     * `WO-` + 6 digits  - the live predicate matches BASE_ID verbatim.
--     * exactly 6 digits  - the live predicate matches it through @WorkOrderBaseId (the stripped
--                           form), so `WO-` + those digits is the key the caller uses.
--   A 5-digit numeric BASE_ID is deliberately EXCLUDED. The application pads it to `WO-0xxxxx`,
--   which the live predicate matches neither verbatim nor stripped, so a cached row for that key
--   would answer an input the live read answers with nothing — the fallback would not be transparent
--   (FR-002/FR-004). Worse, for the three ids that also exist as a `WO-0xxxxx` order the padded key
--   would answer with a DIFFERENT order than the live read returns. Measured live on 2026-09-11:
--   76 open 5-digit numeric orders, 3 of them colliding; excluding them leaves 154 + 555 = 709
--   addressable keys (701 of them carrying a PART_ID) where cache and live agree.
--   OPEN ITEM (not a cache concern): the application's `^(?:WO-)?(\d{5,6})$` rule cannot express
--   42,143 of the 42,852 open orders at all (the `Q`, non-numeric-`M` and 7+-digit families), so
--   widening coverage is an operator decision about the application's input rule, not about this read.
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
        -- Addressable work orders only. The mirror must hold exactly the keys the application can ask
        -- for AND the live per-key read (LookupWorkOrder.sql) resolves, or the fallback is not
        -- transparent (FR-002/FR-004): a key the live read cannot answer would let an outage serve an
        -- order that a healthy Infor Visual never returns for that input.
        --   * 'W' form  - BASE_ID is literally 'WO-' + 6 digits. The live read matches it verbatim
        --                 (@NormalizedWorkOrderTrimmed), so the key is the BASE_ID as stored.
        --   * numeric form - BASE_ID is exactly 6 digits. The application normalizes the operator's
        --                 6-digit input to 'WO-' + those digits, and the live read matches it through
        --                 @WorkOrderBaseId (the stripped form), so the key is 'WO-' + the BASE_ID.
        -- A 5-digit numeric BASE_ID is deliberately excluded: the application would pad it to
        -- 'WO-0xxxxx', which the live read matches neither verbatim nor stripped, so caching it would
        -- put an order in the mirror that no live read can corroborate (and for the three ids that
        -- also exist as 'WO-0xxxxx', the padded key would answer with a DIFFERENT order).
        AND (
            (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
             AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
             AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%')
            OR
            (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 6
             AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%')
        )
)
SELECT DISTINCT
    CASE
        WHEN LTRIM(RTRIM(ow.BaseId)) LIKE 'WO-%' THEN LTRIM(RTRIM(ow.BaseId))
        ELSE 'WO-' + LTRIM(RTRIM(ow.BaseId))
    END AS NormalizedWorkOrder,
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
