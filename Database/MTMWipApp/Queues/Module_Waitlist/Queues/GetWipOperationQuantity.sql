-- ========================================
-- Module: Waitlist (generic work center request, stated part)
-- MySQL Queue Script: Get WIP Operation Quantity
-- Purpose: Return how much of one part the WIP floor holds at one operation, and how many locations
--          it holds any at, so an open sequence's quantity comes from the floor (spec 017 FR-009,
--          FR-013).
-- Target: MTM WIP Application MySQL (mtm_wip_application_winforms)
-- Tables: inv_inventory (PartID, Operation, Quantity, ItemType)
-- Parameters:
--   @PartNumber
--   @Operation - the chosen sequence
--
-- One row is always returned. OperationQuantity is the sum over every location the floor holds the
-- part at that operation, and 0 is a real answer: the floor holds none (FR-013).
--
-- LocationCount is what separates "the floor holds none" from "the floor holds the part elsewhere":
-- it counts the rows behind the sum, so the caller can say the floor answered with nothing rather than
-- silently treating an empty result as zero. Both columns are facts read from the floor; neither is
-- ever derived from the other system.
--
-- Dunnage rows are excluded, as in every other floor script: they are packaging stock, not product.
-- ========================================

SELECT
    COALESCE(SUM(inv.Quantity), 0) AS OperationQuantity,
    COUNT(*) AS LocationCount
FROM inv_inventory AS inv
WHERE inv.PartID IS NOT NULL
  AND LTRIM(RTRIM(inv.PartID)) = LTRIM(RTRIM(@PartNumber))
  AND inv.ItemType <> 'Dunnage'
  AND CAST(LTRIM(RTRIM(inv.Operation)) AS UNSIGNED) = CAST(LTRIM(RTRIM(@Operation)) AS UNSIGNED);
