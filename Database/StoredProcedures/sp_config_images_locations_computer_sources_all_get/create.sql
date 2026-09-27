-- Stored Procedure: sp_config_images_locations_computer_sources_all_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
--
-- Purpose: this machine's picture sources *including the ones that have been withdrawn*, so the readiness check
--          can tell "never given its folders" apart from "its folders were removed" (FR-004, FR-009).
--
-- Contract:
--   IN  p_computer_id  BIGINT  the machine, from `core_computers_registry.id`
--   OUT zero to three rows: source_kind, image_path, is_active — live rows first, then by source kind
--
-- Why this exists beside sp_config_images_locations_computer_sources_get
-- ---------------------------------------------------------------------
-- That read filters `is_active = 1`, because the machine's *current* folders are the live ones. The diagnosis
-- needs a second and different answer: whether this machine was ever given folders at all. A machine with no
-- rows has never been configured; a machine whose rows have all been withdrawn had a configuration that was
-- removed; and only a read that returns the withdrawn rows can tell those two apart. Adding an `is_active`
-- filter here, or a second `_get` that duplicated this one with different WHERE clause, would collapse the two
-- answers into one and make FR-004's "specific to the cause" impossible to honour.
--
-- It reads one machine's rows and only that machine's. `computer_id` is the machine column T051 added, and every
-- row in the `computer` scope carries it, so no identifier is parsed here to work out whose row it is looking at.
-- `scope = 'computer'` is kept as well, so a future scope that happened to use a machine id cannot leak into this
-- answer.
--
-- `source_kind` is derived from the item id exactly as the `_get` read derives it — the shape is documented on
-- the table and is `<computer_id>:<source_kind>` — so the two reads cannot disagree about a row's kind.
--
-- This is a read and returns a result set. It owns no data, so its rollback drops nothing but the routine.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_all_get;

CREATE PROCEDURE sp_config_images_locations_computer_sources_all_get(
    IN p_computer_id BIGINT
)
SELECT
    SUBSTRING_INDEX(i.scope_item_id, ':', -1) AS source_kind,
    i.image_path,
    i.is_active
FROM
    config_images_locations i
WHERE
    i.scope = 'computer'
    AND i.computer_id = p_computer_id
ORDER BY
    i.is_active DESC,
    SUBSTRING_INDEX(i.scope_item_id, ':', -1);
