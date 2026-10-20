-- ========================================
-- Module: Waitlist
-- MySQL Queue Script: Get WIP Inventory Locations
-- Purpose: Return every location the MTM WIP Application holds stock of one part in, with the quantity
--          held at each, for the part-number stock lookup on the Deliver -> Component card.
-- Target: MTM WIP Application MySQL (mtm_wip_application_winforms)
-- Tables: inv_inventory (PartID, Location, Quantity, ItemType)
-- Parameters:
--   @PartNumber
--
-- Dunnage rows (ItemType='Dunnage') are excluded - they are packaging stock, not product.
-- Locations with no stock are not returned.
-- ========================================

SELECT
    inv.Location AS Location,
    COALESCE(SUM(inv.Quantity), 0) AS Quantity
FROM inv_inventory AS inv
WHERE inv.PartID = LTRIM(RTRIM(@PartNumber))
  AND inv.ItemType <> 'Dunnage'
GROUP BY inv.Location
HAVING COALESCE(SUM(inv.Quantity), 0) > 0
ORDER BY inv.Location;
