-- Stored Procedure: sp_config_images_locations_history_get
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T012)
--
-- Purpose: read one picture's change record, newest first, with the actor's display name (FR-027).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)   the system the picture belongs to
--   IN  p_scope_item_id  VARCHAR(190)  the item code, category code, work centre id or part number
--   OUT a result set of previous_image_path, new_image_path, changed_by_user_id, changed_by_display_name,
--       changed_utc — one row per set or replace, newest first. It is a read and returns a result set, so it goes
--       through the query seam rather than the non-query one.
--
-- `changed_by_display_name` is joined rather than stored: the name a person is shown must be the name the person
-- has today, and a row that is NULL because the actor's profile was retired answers with an empty name rather
-- than losing the record. The actor's id and the moment are copied into the history row itself, so the record
-- survives its actor.
--
-- The ORDER is newest first and `id DESC` breaks a tie: two changes can share a second, and a record that showed
-- them in an arbitrary order would be a record that reported the wrong final picture.
--
-- The index idx_config_images_locations_history_scope_item (scope, scope_item_id, changed_utc) matches this
-- filter and this ordering left to right, which is why it is shaped that way rather than on the actor.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_history_get;

CREATE PROCEDURE sp_config_images_locations_history_get(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190)
)
SELECT
    h.previous_image_path,
    h.new_image_path,
    h.changed_by_user_id,
    COALESCE(u.display_name, '') AS changed_by_display_name,
    h.changed_utc
FROM
    config_images_locations_history h
    LEFT JOIN core_users_profiles u ON u.id = h.changed_by_user_id
WHERE
    h.scope = p_scope
    AND h.scope_item_id = p_scope_item_id
ORDER BY
    h.changed_utc DESC,
    h.id DESC;
