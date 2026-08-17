-- ========================================
-- Module: Setup
-- Queue Script: Lookup Work Order
-- Purpose: Resolve part numbers and primary work centers for a normalized work order
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @NormalizedWorkOrder
-- ========================================

SET NOCOUNT ON;

DECLARE @WorkOrderBaseId nvarchar(30) =
	CASE
		WHEN CHARINDEX('-', @NormalizedWorkOrder) > 0
			THEN SUBSTRING(@NormalizedWorkOrder, CHARINDEX('-', @NormalizedWorkOrder) + 1, LEN(@NormalizedWorkOrder))
		ELSE LTRIM(RTRIM(@NormalizedWorkOrder))
	END;

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
	wo.BASE_ID IN (@NormalizedWorkOrderTrimmed, @WorkOrderBaseId)
	AND wo.PART_ID IS NOT NULL
ORDER BY
	wo.PART_ID;