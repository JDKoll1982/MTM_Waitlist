-- Stored Procedure: sp_config_images_locations_delete_by_public_id
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T094)
--
-- Purpose: withdraw one override addressed by its public identifier. Replaces the inline UPDATE in
--          `ImageOverrideWriteService.DeleteByPublicIdAsync` (FR-015). Soft delete, for the same reason as
--          `sp_config_images_locations_delete`.
--
-- Contract:
--   IN  p_public_id  CHAR(36)
--   IN  p_user_id    BIGINT     NULL when the caller has no user
--   OUT affected row count; 0 is the caller's NOT_FOUND answer.
--
-- **The return value is the whole reason this site had to change.** The caller previously ran this UPDATE through
-- the row-returning query helper and decided success from `rows.Count > 0`. An UPDATE returns no rows, so that
-- count was always 0 and the method reported NOT_FOUND for every call — including the ones where the row really
-- was withdrawn. Routing through the non-query helper is what makes the affected-row count available; the
-- predicate itself (`public_id` + `is_active = 1`) is unchanged.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_delete_by_public_id;

CREATE PROCEDURE sp_config_images_locations_delete_by_public_id(
    IN p_public_id CHAR(36),
    IN p_user_id BIGINT
)
UPDATE config_images_locations
SET is_active = 0,
    updated_by_user_id = p_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE public_id = p_public_id
  AND is_active = 1;
