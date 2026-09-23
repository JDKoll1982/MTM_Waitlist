-- Stored Procedure: sp_config_images_locations_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094), history row added by 008-part-pictures (task T015)
--
-- Purpose: repoint one live override at a different image. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.UpdateOverrideAsync` (FR-015).
--
-- Contract:
--   IN  p_scope          VARCHAR(16)
--   IN  p_scope_item_id  VARCHAR(190)
--   IN  p_image_path     VARCHAR(500)
--   IN  p_user_id        BIGINT        NULL when the caller has no user
--   OUT affected row count.
--
-- `AND is_active = 1` is kept from the statement this replaces: an update is only meaningful for a live override,
-- and the caller has already reported NOT_FOUND when it could not read one. `created_*` columns are untouched —
-- repointing an image is not a new row.
--
-- ============================================================
-- 008-part-pictures: the picture change record (FR-027, task T015)
-- ============================================================
-- A replacement writes one row into `config_images_locations_history` carrying the path it replaced. That row is
-- written by the `AFTER UPDATE` trigger below rather than by this procedure, for the same reason T014 records: this
-- procedure's answer to its caller is its affected-row count, and a CALL reports the count of the last statement
-- it executed, so a body ending in COMMIT would report 0 and every picture write would be read as a database
-- error. A trigger leaves this statement — and therefore its signature and its answer — exactly as they were,
-- while the history row is written inside the same statement and so inside the same transaction.
--
-- Two guards on the trigger, and both matter:
--   * `NOT (OLD.image_path <=> NEW.image_path)` — only a path that actually changed is a picture change. Clearing
--     `is_active` to withdraw a picture updates the row without changing its path, and the change record has
--     nothing to say about it.
--   * `@mtm_picture_layout_move IS NULL` — the recorded-path move (sp_config_images_locations_paths_move, T021)
--     rewrites every image_path in the store as a layout migration. Without this guard that one reviewed step
--     would write a change row per stored picture, each naming an actor who never touched it.
-- ============================================================

USE mtm_waitlist;

DROP TRIGGER IF EXISTS trg_config_images_locations_history_on_update;

DROP PROCEDURE IF EXISTS sp_config_images_locations_update;

CREATE PROCEDURE sp_config_images_locations_update(
    IN p_scope VARCHAR(16),
    IN p_scope_item_id VARCHAR(190),
    IN p_image_path VARCHAR(500),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET image_path = p_image_path,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE scope = p_scope
  AND scope_item_id = p_scope_item_id
  AND is_active = 1;

-- DELIMITER because a trigger body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE TRIGGER trg_config_images_locations_history_on_update
AFTER UPDATE ON config_images_locations
FOR EACH ROW
BEGIN
    IF NOT (OLD.image_path <=> NEW.image_path) AND @mtm_picture_layout_move IS NULL THEN
        INSERT INTO config_images_locations_history (
            public_id,
            image_location_id,
            scope,
            scope_item_id,
            previous_image_path,
            new_image_path,
            changed_by_user_id,
            changed_utc
        )
        VALUES (
            UUID(),
            NEW.id,
            NEW.scope,
            NEW.scope_item_id,
            OLD.image_path,
            NEW.image_path,
            NEW.updated_by_user_id,
            NEW.updated_utc
        );
    END IF;
END$$

DELIMITER ;
