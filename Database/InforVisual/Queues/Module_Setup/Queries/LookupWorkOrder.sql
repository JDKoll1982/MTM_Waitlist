-- ========================================
-- Module: Setup
-- Queue Script: Lookup Work Order
-- Purpose: Resolve part numbers and primary work centers for a normalized work order
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @NormalizedWorkOrder - the `WO-######` key, matched verbatim against WORK_ORDER.BASE_ID
-- ========================================

SET NOCOUNT ON;

-- The application sends only the canonical `WO-######` key (it accepts lenient operator input and
-- autoformats it first, so nothing downstream ever sees the raw text; operator decision 2026-09-12,
-- task T144) and passes that same string to both the live read and the cached copy, so this predicate
-- matches the BASE_ID verbatim. It must NOT also match a stripped base id: that fallback resolved
-- bare 6-digit `M`-family orders the application can no longer ask for, and where an id also existed
-- as a `WO-0xxxxx` order it returned a DIFFERENT order than the cache served (the T144 cross-order
-- collisions). Cache and live must stay keyed by exactly the same string.
DECLARE @NormalizedWorkOrderTrimmed nvarchar(30) = LTRIM(RTRIM(@NormalizedWorkOrder));

SELECT DISTINCT
	wo.PART_ID AS PartNumber,
	COALESCE(part.DESCRIPTION, '') AS Description,
	COALESCE(op.RESOURCE_ID, '') AS WorkCenter
FROM WORK_ORDER AS wo
LEFT JOIN PART AS part
	ON part.ID = wo.PART_ID
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
	wo.BASE_ID = @NormalizedWorkOrderTrimmed
	AND wo.PART_ID IS NOT NULL
ORDER BY
	wo.PART_ID;