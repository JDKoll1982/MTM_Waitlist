-- Stored Procedure: sp_config_images_locations_computer_sources_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T051)
--
-- Purpose: the picture sources of one machine — where it reads its stored pictures, its shared keys and its
--          dunnage pictures from.
--
-- Contract:
--   IN  p_computer_id  BIGINT  the machine, from `core_computers_registry.id`
--   OUT zero to three rows: source_kind, image_path, ordered by source kind
--
-- One machine's rows and not one scope's. Every other reader of this table takes a scope, because a scope is a
-- family of pictures; this one takes a machine, because a machine's folders are its own and the three rows it
-- holds are the only rows in the `computer` scope that belong to it. That is why the read filters on
-- `computer_id` and does not parse identifiers to work out whose row it is looking at.
--
-- `source_kind` is derived from the item id rather than stored, because the item id has to carry the machine —
-- the table's unique key is over (scope, item id), and a machine holding three rows in one scope can only do
-- that if the three item ids differ. The shape is `<computer_id>:<source_kind>` and it is documented on the
-- table; deriving the kind here keeps the two forms from being written down twice.
--
-- No row means the machine has never been given its folders, which machine setup treats as unconfigured rather
-- than as configured-empty: the three sources are required, and a machine with no answer is asked for one.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_get;

CREATE PROCEDURE sp_config_images_locations_computer_sources_get(
    IN p_computer_id BIGINT
)
SELECT
    SUBSTRING_INDEX(i.scope_item_id, ':', -1) AS source_kind,
    i.image_path
FROM
    config_images_locations i
WHERE
    i.scope = 'computer'
    AND i.computer_id = p_computer_id
    AND i.is_active = 1
ORDER BY
    SUBSTRING_INDEX(i.scope_item_id, ':', -1);
