-- Seed: seed_waitlist_request_item_configs_label_move
-- Engine: MySQL 5.7
-- Purpose: Move the four declaration labels the card reads onto the spellings it reads them under, in place, so
--          an installed store gets `Die Location`, `Home Location`, `Scrap Type` and `Coil Number` without the
--          truncate-and-reinsert of Database/Seeds/seed_waitlist_request_item_configs wiping operator edits
--          (D33, FR-059).
-- Scope:   1. `Pickup location` to `Die Location` on pickup-die and deliver-die.
--          2. `Home location` to `Home Location` on pickup-dunnage and deliver-dunnage.
--          3. `Scrap type` to `Scrap Type` on pickup-scrap.
--          4. `Coil number` to `Coil Number` on assist-coil-turn only. The coil Items keep `Coil number`, which
--             is the label their own cell is declared and read under, so this move is scoped to the one Item
--             whose cell is spelled `Coil Number` (data-model.md §9).
--          5. `Part number` to `Part` on pickup-fg, and 6. `Part and quantity` to `Part` on pickup-wip, so the
--             four returning pickups all draw the part the work centre is making under one label.
-- Method:  Each statement rewrites one label inside detail_fields_json through JSON_SEARCH, so the path is the
--          path the label actually sits at rather than a guessed array index: a store whose fields were added,
--          removed or reordered still moves the label it holds, and a store that does not hold it is left
--          untouched. Every other key of that field, every other field, and every other column of the row are
--          left exactly as they were; only the label and the row's updated_utc change.
-- Rerunnable: The guard is the presence of the label being moved, so a second run matches no row and changes
--          nothing. The statements are the only writes, and there is no TRUNCATE anywhere in this file.
-- Not in scope: The cells this feature adds (`Die Shop Direction`, `Size`, `# of Skids`, `Weight of Coil`,
--          `Part Type`, `Item Type`, `Total Qty In House`, `Lugger Location`) and the two source corrections on
--          the wrong-part Items are declarations rather than labels, and they arrive with
--          Database/Seeds/seed_waitlist_request_item_configs, which the installer applies. This file moves
--          labels only, so a store that is updated by hand never has its field set rewritten wholesale.

USE mtm_waitlist;

SET NAMES utf8mb4;

-- 1. The die Items' location label.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Pickup location', NULL, '$[*].label')),
        'Die Location'),
    updated_utc = UTC_TIMESTAMP()
WHERE item IN ('pickup-die', 'deliver-die')
  AND JSON_SEARCH(detail_fields_json, 'one', 'Pickup location', NULL, '$[*].label') IS NOT NULL;

-- 2. The dunnage Items' home-location label.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Home location', NULL, '$[*].label')),
        'Home Location'),
    updated_utc = UTC_TIMESTAMP()
WHERE item IN ('pickup-dunnage', 'deliver-dunnage')
  AND JSON_SEARCH(detail_fields_json, 'one', 'Home location', NULL, '$[*].label') IS NOT NULL;

-- 3. The scrap Item's type label.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Scrap type', NULL, '$[*].label')),
        'Scrap Type'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-scrap'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Scrap type', NULL, '$[*].label') IS NOT NULL;

-- 4. The assist coil turn's coil label. Scoped to that one Item: the coil Items' own cell is `Coil number`.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Coil number', NULL, '$[*].label')),
        'Coil Number'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'assist-coil-turn'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Coil number', NULL, '$[*].label') IS NOT NULL;

-- 5. The finished-goods pickup's part label: the card reads the part the work centre is making as `Part`.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Part number', NULL, '$[*].label')),
        'Part'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-fg'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Part number', NULL, '$[*].label') IS NOT NULL;

-- 6. The work-in-process pickup's part label, for the same reason.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Part and quantity', NULL, '$[*].label')),
        'Part'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-wip'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Part and quantity', NULL, '$[*].label') IS NOT NULL;
