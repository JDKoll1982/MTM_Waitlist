-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Operation sequences (shape 2 of 5: operation_sequences)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 2 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope: for every ADDRESSABLE open work order (STATUS in 'R','U','F'; see the addressability guard
--        below) its own order part's operation sequences that carry at least one requirement row —
--        exactly the rows GetSequences.sql returns for that (work order, part) pair.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_operation_sequences_refresh):
--   NormalizedWorkOrder, PartNumber (input keys), SequenceNumber, Description (outputs).
-- ========================================

SET NOCOUNT ON;

SELECT DISTINCT
    CASE
        WHEN LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%' THEN LTRIM(RTRIM(wo.BASE_ID))
        ELSE 'WO-' + LTRIM(RTRIM(wo.BASE_ID))
    END AS NormalizedWorkOrder,
    wo.PART_ID AS PartNumber,
    o.SEQUENCE_NO AS SequenceNumber,
    CONCAT('Operation ', o.SEQUENCE_NO, ' / ', COALESCE(NULLIF(o.RESOURCE_ID, ''), 'Unassigned')) AS Description
FROM WORK_ORDER AS wo
INNER JOIN OPERATION AS o
    ON o.WORKORDER_TYPE = wo.TYPE
    AND o.WORKORDER_BASE_ID = wo.BASE_ID
    AND o.WORKORDER_LOT_ID = wo.LOT_ID
    AND o.WORKORDER_SPLIT_ID = wo.SPLIT_ID
    AND o.WORKORDER_SUB_ID = wo.SUB_ID
WHERE
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    -- Addressable work orders only: the two forms the application can ask for AND the live read
    -- (GetSequences.sql) resolves. Full rationale in work_order_lookup_population.sql.
    AND (
        (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
         AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
         AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%')
        OR
        (LEN(LTRIM(RTRIM(wo.BASE_ID))) = 6
         AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%')
    )
    AND EXISTS
    (
        SELECT 1
        FROM REQUIREMENT AS req
        WHERE req.WORKORDER_TYPE = wo.TYPE
            AND req.WORKORDER_BASE_ID = wo.BASE_ID
            AND req.WORKORDER_LOT_ID = wo.LOT_ID
            AND req.WORKORDER_SPLIT_ID = wo.SPLIT_ID
            AND req.WORKORDER_SUB_ID = wo.SUB_ID
            AND req.OPERATION_SEQ_NO = o.SEQUENCE_NO
            AND req.PART_ID IS NOT NULL
    )
ORDER BY
    NormalizedWorkOrder,
    PartNumber,
    SequenceNumber;
