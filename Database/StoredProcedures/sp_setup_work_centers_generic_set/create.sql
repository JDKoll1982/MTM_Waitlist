-- Stored Procedure: sp_setup_work_centers_generic_set
-- Engine: MySQL 5.7
-- Feature: 016-generic-wc-request-foundation (FR-001, FR-002, FR-004)
--
-- Purpose: set or clear one work center's generic designation.
--
-- Why this is its own procedure rather than a parameter on sp_setup_work_centers_upsert
-- ------------------------------------------------------------------------------------
-- The upsert takes the id, building and name because it serves the add and edit dialogs. The
-- designation is a separate decision, taken from its own control under its own rule, and it is the only
-- write in the setup flow that must not be able to change a name or a building as a side effect. Routing
-- it through the upsert would have made every designation change carry the whole row, and it would have
-- changed a procedure other call sites already depend on.
--
-- The designation is never set for anyone by a build, a default or a migration: a fresh install leaves
-- every work center at is_generic = 0 (FR-003), and clearing the mark leaves already-raised requests alone
-- because no request reads this column (FR-004).
--
-- Contract:
--   IN  p_work_center_id        the row's id, as text, matching sp_setup_work_centers_delete
--   IN  p_is_generic            1 to mark, anything else to clear
--   IN  p_modified_by_user_id   the person making the change, or NULL when the session carries no id
--   OUT affected rows           0 when the id matches no row
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_generic_set;

CREATE PROCEDURE sp_setup_work_centers_generic_set(
    IN p_work_center_id VARCHAR(32),
    IN p_is_generic TINYINT(1),
    IN p_modified_by_user_id BIGINT
)
UPDATE setup_work_centers_catalog
SET
    is_generic = IF(p_is_generic = 0, 0, 1),
    updated_by_user_id = COALESCE(p_modified_by_user_id, updated_by_user_id),
    updated_utc = UTC_TIMESTAMP()
WHERE id = CAST(NULLIF(TRIM(p_work_center_id), '') AS UNSIGNED);
