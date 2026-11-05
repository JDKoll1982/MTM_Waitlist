-- ========================================
-- Module: Waitlist (generic work center request, stated part)
-- MySQL Queue Script: Get WIP Part Operations
-- Purpose: Return every distinct operation the WIP floor holds one part at, so those operations can be
--          listed as sequences beside the ones Infor Visual records (spec 017 FR-004).
-- Target: MTM WIP Application MySQL (mtm_wip_application_winforms)
-- Tables: inv_inventory (PartID, Operation, ItemType)
-- Parameters:
--   @PartNumber
--
-- The operation column is inv_inventory.Operation, varchar(100). Confirmed against the live schema on
-- 2026-11-03: its values are the plant's operation numbers ('20', '90', '19', '30', …) alongside rows
-- carrying the literal 'N/A'.
--
-- A value that is not a usable sequence number yields no sequence (FR-006), so blank, 'N/A' and any
-- non-numeric value are dropped here rather than at the caller. The regular expression is the whole
-- test: MySQL evaluates it as a match, not a search, so 'N/A' and '10A' both fail it.
--
-- Dunnage rows are excluded, as in every other floor script: they are packaging stock, not product.
-- ========================================

SELECT operations.Sequence
FROM (
    SELECT DISTINCT
        CAST(CAST(LTRIM(RTRIM(inv.Operation)) AS UNSIGNED) AS CHAR) AS Sequence
    FROM inv_inventory AS inv
    WHERE inv.PartID IS NOT NULL
      AND LTRIM(RTRIM(inv.PartID)) = LTRIM(RTRIM(@PartNumber))
      AND inv.ItemType <> 'Dunnage'
      AND inv.Operation IS NOT NULL
      AND LTRIM(RTRIM(inv.Operation)) REGEXP '^[0-9]+$'
) AS operations
ORDER BY CAST(operations.Sequence AS UNSIGNED);
