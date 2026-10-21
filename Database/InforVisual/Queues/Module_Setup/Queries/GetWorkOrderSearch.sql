-- ========================================
-- Module: Setup
-- Queue Script: Search Work Orders
-- Purpose: Find the open work orders a setup run can start from, by work-order number, part number or
--          customer name, so the operator can pick one instead of typing it from memory.
-- Target: Infor Visual SQL Server (VISUAL / MTMFG)
-- Parameters:
--   @WorkOrderText  - what the operator typed for the work order, matched FUZZY: the digits are also
--                     matched with any `WO-` prefix and dashes removed, so `0752`, `WO-0752` and
--                     `752` all find WO-0752xx orders.
--   @PartFilter     - part number fragment, matched with LIKE. Empty means "no part constraint".
--   @CustomerFilter - customer NAME fragment (never the customer id), matched with LIKE. Empty means
--                     "no customer constraint".
--
-- Shape: work_order_search (read shape 8). The projection below is the mirror's contract; the
--        population script and sp_visual_work_order_search_get repeat it exactly.
--
-- Domain
-- ------
-- Only the ADDRESSABLE open work orders are searched: STATUS in ('R','U','F') and a BASE_ID of the
-- canonical `WO-` + six-digit form. That is the same domain the work-order entry page can act on
-- (WorkOrderValidationService autoformats to `WO-######`, and LookupWorkOrder.sql matches BASE_ID
-- verbatim), so every row this search returns is a value the entry page accepts and resolves. A row
-- outside it would look selectable and then fail on the next screen, which is why the domain is
-- narrowed here rather than in the dialog.
--
-- Customer
-- --------
-- Infor Visual carries no customer on WORK_ORDER. The customer of a customer-driven order is the
-- allocation demand: CUST_ORDER_ALLOC rows with SUPPLY_TYPE = 'WO' name the work order in BASE_ID and
-- carry the customer's real name in CUST_NAME. The latest allocation is the one shown, and the
-- customer filter is applied inside that same lookup so a filtered row always displays the customer it
-- matched. An order with no allocation (a stock order) simply has no customer, and is excluded when a
-- customer filter is given.
--
-- Fully fuzzy matching (edit distance) is deliberately not used: it cannot use an index and would scan
-- 113 000 work orders per keystroke. Substring matching on the digits, the part and the customer name
-- is what makes `0752` find `WO-0752xx`, and it stays a single indexed-ish scan with TOP applied.
-- ========================================

SET NOCOUNT ON;

DECLARE @WorkOrderTextTrimmed nvarchar(60) = LTRIM(RTRIM(ISNULL(@WorkOrderText, '')));
DECLARE @PartFilterTrimmed nvarchar(60) = LTRIM(RTRIM(ISNULL(@PartFilter, '')));
DECLARE @CustomerFilterTrimmed nvarchar(120) = LTRIM(RTRIM(ISNULL(@CustomerFilter, '')));

-- The work-order fragment with the `WO-` prefix and the dashes taken out. `REPLACE` is
-- case-insensitive under the database collation, so `wo-0752` reduces the same way `WO-0752` does.
DECLARE @WorkOrderDigits nvarchar(60) =
    REPLACE(REPLACE(REPLACE(UPPER(@WorkOrderTextTrimmed), 'WO-', ''), 'WO', ''), '-', '');

-- The canonical key a fragment names when it is 1-6 digits, so an exact hit sorts above a substring hit.
DECLARE @WorkOrderCandidate nvarchar(30) =
    CASE
        WHEN @WorkOrderDigits <> ''
             AND LEN(@WorkOrderDigits) <= 6
             AND @WorkOrderDigits NOT LIKE '%[^0-9]%'
            THEN 'WO-' + RIGHT('000000' + @WorkOrderDigits, 6)
        ELSE ''
    END;

SELECT TOP (200)
    LTRIM(RTRIM(wo.BASE_ID)) AS WorkOrder,
    wo.PART_ID AS PartNumber,
    COALESCE(part.DESCRIPTION, '') AS PartDescription,
    COALESCE(cust.CUST_NAME, '') AS CustomerName,
    COALESCE(op.RESOURCE_ID, '') AS WorkCenter,
    LTRIM(RTRIM(wo.STATUS)) AS Status,
    CAST(wo.DESIRED_WANT_DATE AS date) AS WantDate
FROM WORK_ORDER AS wo
LEFT JOIN PART AS part
    ON part.ID = wo.PART_ID
OUTER APPLY
(
    SELECT TOP (1)
        a.CUST_NAME
    FROM CUST_ORDER_ALLOC AS a
    WHERE
        a.SUPPLY_TYPE = 'WO'
        AND a.BASE_ID = wo.BASE_ID
        AND NULLIF(LTRIM(RTRIM(a.CUST_NAME)), '') IS NOT NULL
        AND (@CustomerFilterTrimmed = '' OR a.CUST_NAME LIKE '%' + @CustomerFilterTrimmed + '%')
    ORDER BY
        a.WANT_DATE DESC,
        a.CUST_NAME
) AS cust
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
    wo.STATUS IN ('R', 'U', 'F')
    AND wo.BASE_ID IS NOT NULL
    AND wo.PART_ID IS NOT NULL
    -- Addressable work orders only — the canonical `WO-` + six digits. See the header.
    AND LEN(LTRIM(RTRIM(wo.BASE_ID))) = 9
    AND LTRIM(RTRIM(wo.BASE_ID)) LIKE 'WO-%'
    AND SUBSTRING(LTRIM(RTRIM(wo.BASE_ID)), 4, 6) NOT LIKE '%[^0-9]%'
    -- Fuzzy work-order match: the typed text, or the same text with `WO-` and dashes removed.
    AND (
        @WorkOrderTextTrimmed = ''
        OR wo.BASE_ID LIKE '%' + @WorkOrderTextTrimmed + '%'
        OR (@WorkOrderDigits <> '' AND wo.BASE_ID LIKE '%' + @WorkOrderDigits + '%')
    )
    AND (@PartFilterTrimmed = '' OR wo.PART_ID LIKE '%' + @PartFilterTrimmed + '%')
    -- The customer constraint is applied inside the lookup, so a matched row displays the customer it matched.
    AND (@CustomerFilterTrimmed = '' OR cust.CUST_NAME IS NOT NULL)
ORDER BY
    CASE
        WHEN @WorkOrderCandidate <> '' AND wo.BASE_ID = @WorkOrderCandidate THEN 0
        WHEN @WorkOrderCandidate <> '' AND wo.BASE_ID LIKE @WorkOrderCandidate + '%' THEN 1
        WHEN @WorkOrderDigits <> '' AND wo.BASE_ID LIKE '%' + @WorkOrderDigits + '%' THEN 2
        ELSE 3
    END,
    wo.DESIRED_WANT_DATE,
    wo.BASE_ID DESC;
