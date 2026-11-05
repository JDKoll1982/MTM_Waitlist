-- ========================================
-- Module: Waitlist (generic work center request, stated part)
-- MySQL Queue Script: Get WIP Inventory Part Matches
-- Purpose: Return the parts the WIP floor holds that a typed term matches, so the generic part step
--          completes a number the floor knows even when Infor Visual does not (spec 017 FR-001,
--          scenario 3 of user story 1).
-- Target: MTM WIP Application MySQL (mtm_wip_application_winforms)
-- Tables: inv_inventory (PartID, ItemType)
-- Parameters:
--   @PartNumber - the term to match. The exact number is returned first, then numbers containing it
--
-- LIVE ONLY, and no mirror: the floor is an operational store and is read live (constitution II).
--
-- LOCATE is used rather than LIKE so the term is matched literally. A part number holding '%' or '_'
-- must match those characters rather than being read as a pattern, and LOCATE takes no wildcards.
--
-- Dunnage rows (ItemType='Dunnage') are excluded for the same reason every other floor script excludes
-- them: they are packaging stock, not product. The floor holds no part description, so the caller
-- renders the number and its source alone.
-- ========================================

SELECT matches.PartNumber
FROM (
    SELECT DISTINCT
        LTRIM(RTRIM(inv.PartID)) AS PartNumber
    FROM inv_inventory AS inv
    WHERE inv.PartID IS NOT NULL
      AND LTRIM(RTRIM(inv.PartID)) <> ''
      AND inv.ItemType <> 'Dunnage'
      AND (
          LTRIM(RTRIM(inv.PartID)) = LTRIM(RTRIM(@PartNumber))
          OR LOCATE(LTRIM(RTRIM(@PartNumber)), LTRIM(RTRIM(inv.PartID))) > 0
      )
) AS matches
ORDER BY
    CASE WHEN matches.PartNumber = LTRIM(RTRIM(@PartNumber)) THEN 0 ELSE 1 END,
    matches.PartNumber
LIMIT 200;
