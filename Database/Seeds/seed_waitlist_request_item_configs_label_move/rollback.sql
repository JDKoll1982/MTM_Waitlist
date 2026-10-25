-- Rollback: seed_waitlist_request_item_configs_label_move
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_waitlist_request_item_configs_label_move/create.sql
-- Purpose:  Restore the four declaration labels to the spellings the store carried before the move, in place,
--           so an environment that promoted the move returns to `Pickup location`, `Home location`,
--           `Scrap type` and `Coil number` without the config seed being rerun (FR-059).
-- Method:   The same JSON_SEARCH rewrite as the create script, with the labels swapped, so it is idempotent in
--           the same way: a store that does not carry the moved spelling is left untouched, and a second run of
--           this file changes nothing. Only the label and the row's updated_utc change.
-- Note:     This restores the labels wherever they currently carry the moved spelling. A store whose labels are
--           already back to the original spellings (because Database/Seeds/seed_waitlist_request_item_configs was
--           reapplied from its previous revision, for instance) is left alone.

USE mtm_waitlist;

SET NAMES utf8mb4;

-- 1. The die Items' location label back to its previous spelling.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Die Location', NULL, '$[*].label')),
        'Pickup location'),
    updated_utc = UTC_TIMESTAMP()
WHERE item IN ('pickup-die', 'deliver-die')
  AND JSON_SEARCH(detail_fields_json, 'one', 'Die Location', NULL, '$[*].label') IS NOT NULL;

-- 2. The dunnage Items' home-location label back to its previous spelling.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Home Location', NULL, '$[*].label')),
        'Home location'),
    updated_utc = UTC_TIMESTAMP()
WHERE item IN ('pickup-dunnage', 'deliver-dunnage')
  AND JSON_SEARCH(detail_fields_json, 'one', 'Home Location', NULL, '$[*].label') IS NOT NULL;

-- 3. The scrap Item's type label back to its previous spelling.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Scrap Type', NULL, '$[*].label')),
        'Scrap type'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-scrap'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Scrap Type', NULL, '$[*].label') IS NOT NULL;

-- 4. The assist coil turn's coil label back to its previous spelling.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Coil Number', NULL, '$[*].label')),
        'Coil number'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'assist-coil-turn'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Coil Number', NULL, '$[*].label') IS NOT NULL;

-- 5. The finished-goods pickup's part label back to its previous spelling.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Part', NULL, '$[*].label')),
        'Part number'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-fg'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Part', NULL, '$[*].label') IS NOT NULL;

-- 6. The work-in-process pickup's part label back to its previous spelling.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Part', NULL, '$[*].label')),
        'Part and quantity'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-wip'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Part', NULL, '$[*].label') IS NOT NULL;
