-- Stored Procedure: sp_config_images_locations_scope_paths_get
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T013)
--
-- Purpose: every active picture path of one scope, as `scope_item_id` and `image_path` pairs. Two callers, and
-- both need the whole scope rather than one row.
--
--   1. The file-name collision check before a save (FR-007). A new picture's file name has to be compared against
--      the names the same scope and the same family folder already hold, case-insensitively, so that two part
--      numbers differing only by letter case are refused rather than silently becoming one file.
--   2. The subtraction that produces the missing-picture list (FR-026): the parts the application can name, minus
--      the rows this returns.
--
-- Reading the whole scope is deliberate. Both callers need to compare against every row, so a per-item procedure
-- would mean one round trip per part on a screen that lists hundreds. A scope holds the pictures for one system,
-- which is the unit both callers work in.
--
-- Only active rows are returned: an inactive row is not a picture, and a withdrawn picture must not hold a file
-- name against a part that wants it.
--
-- Contract:
--   IN  p_scope  VARCHAR(16)
--   OUT a result set of scope_item_id, image_path, ordered by scope_item_id so a caller that diffs two reads
--       gets a stable order.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_scope_paths_get;

CREATE PROCEDURE sp_config_images_locations_scope_paths_get(
    IN p_scope VARCHAR(16)
)
SELECT
    i.scope_item_id,
    i.image_path
FROM
    config_images_locations i
WHERE
    i.scope = p_scope
    AND i.is_active = 1
ORDER BY
    i.scope_item_id;
