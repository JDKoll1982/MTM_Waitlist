SET NOCOUNT ON;

DECLARE @CutoffDate datetime = DATEADD(MONTH, -3, GETDATE());

;WITH ResourceLaborStatus AS
(
    SELECT
        sr.ID AS PressOrResourceId,
        sr.DESCRIPTION AS PressOrResourceDescription,
        labor_match.EMPLOYEE_ID AS ActiveEmployeeId,
        LTRIM(
            RTRIM(
                CONCAT(
                    emp.FIRST_NAME,
                    ' ',
                    emp.LAST_NAME
                )
            )
        ) AS ActiveEmployeeName,
        labor_match.USER_ID AS BarcodeUserId,
        labor_match.WORKORDER_TYPE AS WorkOrderType,
        labor_match.WORKORDER_BASE_ID AS WorkOrderBaseId,
        labor_match.WORKORDER_LOT_ID AS WorkOrderLotId,
        labor_match.WORKORDER_SPLIT_ID AS WorkOrderSplitId,
        labor_match.WORKORDER_SUB_ID AS WorkOrderSubId,
        labor_match.OPERATION_SEQ_NO AS OperationSequence,
        labor_match.CLOCK_IN AS LoginTime,
        COALESCE(labor_match.ACT_CLOCK_OUT, labor_match.CLOCK_OUT) AS LastActivity,
        CASE
            WHEN labor_match.ROWID IS NOT NULL
             AND COALESCE(labor_match.ACT_CLOCK_OUT, labor_match.CLOCK_OUT) IS NULL THEN 1
            ELSE 0
        END

AS IsActive
    FROM SHOP_RESOURCE AS sr
    OUTER APPLY
    (
        SELECT TOP (1)
            lt.ROWID,
            lt.EMPLOYEE_ID,
            lt.USER_ID,
            lt.WORKORDER_TYPE,
            lt.WORKORDER_BASE_ID,
            lt.WORKORDER_LOT_ID,
            lt.WORKORDER_SPLIT_ID,
            lt.WORKORDER_SUB_ID,
            lt.OPERATION_SEQ_NO,
            lt.CLOCK_IN,
            lt.CLOCK_OUT,
            lt.ACT_CLOCK_IN,
            lt.ACT_CLOCK_OUT,
            lt.TRANSACTION_DATE
        FROM LABOR_TICKET AS lt
        WHERE
            lt.RESOURCE_ID = sr.ID
            AND lt.TRANSACTION_DATE >= @CutoffDate
            AND lt.WORKORDER_BASE_ID LIKE '%WO-%'
        ORDER BY
            COALESCE(lt.ACT_CLOCK_IN, lt.CLOCK_IN, lt.TRANSACTION_DATE) DESC,
            lt.ROWID DESC
    ) AS labor_match
    LEFT JOIN EMPLOYEE AS emp
        ON emp.ID = labor_match.EMPLOYEE_ID
    WHERE
        COALESCE(sr.STATUS, 'A') <> 'X'
        AND sr.ID NOT IN ('500-GRP', 'OUTSIDE_SERVICE', 'Materials')
)
SELECT
    PressOrResourceId,
    PressOrResourceDescription,
    ActiveEmployeeId,
    ActiveEmployeeName,
    BarcodeUserId,
    WorkOrderType,
    WorkOrderBaseId,
    WorkOrderLotId,
    WorkOrderSplitId,
    WorkOrderSubId,
    OperationSequence,
    LoginTime,
    LastActivity,
    CASE
        WHEN IsActive = 1 THEN 'Active'
        ELSE 'Inactive'
    END AS ResourceStatus
FROM ResourceLaborStatus
ORDER BY
    IsActive DESC,
    LastActivity DESC,
    PressOrResourceDescription,
    PressOrResourceId;