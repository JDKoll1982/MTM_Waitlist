-- ========================================
-- Module: Mock cache (Module_Mock)
-- Population Script: Operation sequences (shape 2 of 5: operation_sequences)
-- Purpose: Produce the COMPLETE mirror snapshot for shape 2 in one set-based read.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters: none (the driver population is enumerated here)
--
-- Scope: for every OPEN work order (STATUS in 'R','U','F') its own order part's operation
--        sequences that carry at least one requirement row — exactly the rows GetSequences.sql
--        returns for that (work order, part) pair.
--
-- Projection contract (must match VisualReadShapeCatalog + sp_visual_operation_sequences_refresh):
--   NormalizedWorkOrder, PartNumber (input keys), SequenceNumber, Description (outputs).
-- ========================================

SET NOCOUNT ON;

SELECT DISTINCT
    'WO-' + RIGHT('000000' + LTRIM(RTRIM(wo.BASE_ID)), 6) AS NormalizedWorkOrder,
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
    AND LEN(LTRIM(RTRIM(wo.BASE_ID))) BETWEEN 5 AND 6
    AND LTRIM(RTRIM(wo.BASE_ID)) NOT LIKE '%[^0-9]%'
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
