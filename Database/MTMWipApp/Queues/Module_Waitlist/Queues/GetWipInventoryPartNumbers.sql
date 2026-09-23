-- ========================================
-- Module: Waitlist
-- MySQL Queue Script: Get WIP Inventory Part Numbers
-- Purpose: Return every distinct part number the MTM WIP Application holds floor inventory for, so a
--          part the WIP floor can name is a part this application can picture (FR-001). Feeds the
--          part-picture coverage read, which subtracts the pictured rows from this list to answer "the
--          parts that still have no picture".
-- Target: MTM WIP Application MySQL (mtm_wip_application_winforms)
-- Tables: inv_inventory (PartID, ItemType)
-- Parameters: none
--
-- Dunnage rows (ItemType='Dunnage') are excluded here for the same reason GetWipFloorQuantities excludes
-- them: they are packaging stock, not product. A part number that is present but blank is not a part the
-- application can name, so it is left out rather than offered as a row with no key.
-- ========================================

SELECT DISTINCT
    LTRIM(RTRIM(inv.PartID)) AS PartNumber
FROM inv_inventory AS inv
WHERE inv.PartID IS NOT NULL
  AND LTRIM(RTRIM(inv.PartID)) <> ''
  AND inv.ItemType <> 'Dunnage'
ORDER BY PartNumber;
