-- Stored Procedure: sp_machine_configuration_reset
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
--
-- Purpose: restore this machine's configuration to its defaults, and only the parts it is told are broken
--          (FR-018, FR-019; contracts/machine-configuration-contract.md section 4).
--
-- Contract:
--   IN  p_computer_id             BIGINT        the machine, from `core_computers_registry.id`
--   IN  p_actor_user_id           BIGINT        the person driving the reset; NULL when there is none
--   IN  p_default_display_name    VARCHAR(128)  the name a reset display name is restored to
--   IN  p_reset_display_name      TINYINT       1 to restore the display name, 0 to leave it
--   IN  p_reset_description       TINYINT       1 to clear the description, 0 to leave it
--   IN  p_reset_picture_sources   TINYINT       1 to withdraw this machine's picture sources, 0 to leave them
--   IN  p_reset_scoped_preferences TINYINT      1 to remove this machine's scoped preference rows, 0 to leave them
--   OUT the affected-row count, through the non-query seam. A reset that matched nothing is a successful no-op,
--       exactly like the other writes in this repository.
--
-- One procedure rather than three, and why
-- ----------------------------------------
-- A reset spans three tables — `core_computers_registry` for the identity half, `config_images_locations` for the
-- picture sources, and `config_settings_values` for the machine's scoped preferences — and a reset that stopped
-- half way would leave a machine in a state none of the three can express on its own: a name with no folders, or
-- folders with no name, which the readiness check would have to call "removed" when nobody removed anything. So
-- the three writes are one transaction behind one call, and this procedure is the single writer of a machine's
-- configuration reset. Its name is descriptive rather than table-prefixed for that reason: there is no one table
-- it belongs to. (The repository's procedure names are lower snake_case and within the 64-character limit; no
-- banned term appears in this one.)
--
-- What it may never touch, enforced by the WHERE clauses and not by convention
-- -------------------------------------------------------------------------
-- Every statement below is filtered to `p_computer_id`:
--   * `core_computers_registry` by `id`, so no other machine's name or description can be reached;
--   * `config_images_locations` by `computer_id` (and `scope = 'computer'`, so a future scope carrying a machine
--     id cannot be caught), so no other machine's folders are withdrawn;
--   * `config_settings_values` by `computer_id`, so no other machine's preference — and no person-scoped or
--     role-scoped row, which carry a NULL `computer_id` — is removed.
-- Nothing here reads or writes a person, a role, a permission, a session or a log entry. In particular it does
-- not delete the registry row itself: that row is also referenced by `user_active_sessions` and
-- `auth_remembered_sign_ins` through foreign keys that would refuse the delete, and clearing either of those is
-- explicitly outside what a reset may do (section 4, "must never touch … any session").
--
-- Why the display name is restored rather than emptied
-- ---------------------------------------------------
-- `core_computers_registry.display_name` is NOT NULL and carries `uq_core_computers_registry_display_name`, so it
-- cannot be emptied: a second machine restored the same way would collide on that key and the reset would fail.
-- The default this procedure restores is therefore the machine's own name, which is what it is known by before
-- anybody gives it a friendlier one and what machine setup pre-fills. The caller checks the default is free
-- before calling here (sp_core_computers_registry_display_name_get), so a taken default is a clean refusal in the
-- application rather than a duplicate-key error raised out of a write.
--
-- Why picture sources are withdrawn rather than deleted
-- ----------------------------------------------------
-- `is_active = 0` is this table's own word for a withdrawn override, and it is what keeps the three rows from
-- being re-inserted as duplicates: `uq_config_images_locations_scope_item` spans (scope, item id) regardless of
-- `is_active`, so a machine that is set up again has its three rows replaced in place and reactivated by
-- sp_config_images_locations_computer_sources_set. The withdrawn rows are also what make the readiness check
-- answer "configuration removed" rather than "never configured" for this machine (FR-009), which is the honest
-- report of what a reset just did.
--
-- Why the scoped-preference delete is safe
-- ----------------------------------------
-- `config_settings_history.config_setting_id` references `config_settings_values.id`, and the foreign key has no
-- ON DELETE rule, so a value row named by a history row cannot be removed. The only writer of that history table
-- is sp_config_permissions_user_set, and it writes `scope_type = 'user'` rows with a NULL `computer_id`. A row
-- scoped to a machine therefore cannot be named by a history row, and this delete cannot silently destroy an
-- audit record. If that ever changed, the foreign key would refuse the delete loudly here rather than the record
-- being lost — which is the safe direction for that to fail in.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_machine_configuration_reset;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_machine_configuration_reset(
    IN p_computer_id BIGINT,
    IN p_actor_user_id BIGINT,
    IN p_default_display_name VARCHAR(128),
    IN p_reset_display_name TINYINT,
    IN p_reset_description TINYINT,
    IN p_reset_picture_sources TINYINT,
    IN p_reset_scoped_preferences TINYINT
)
BEGIN
    DECLARE v_now_utc DATETIME;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_now_utc = fn_server_utc_now();

    START TRANSACTION;

    IF p_reset_picture_sources = 1 THEN
        UPDATE config_images_locations
        SET is_active = 0,
            updated_by_user_id = p_actor_user_id,
            updated_utc = v_now_utc
        WHERE scope = 'computer'
          AND computer_id = p_computer_id
          AND is_active = 1;
    END IF;

    IF p_reset_display_name = 1 OR p_reset_description = 1 THEN
        UPDATE core_computers_registry
        SET display_name = CASE
                WHEN p_reset_display_name = 1 THEN p_default_display_name
                ELSE display_name
            END,
            description = CASE
                WHEN p_reset_description = 1 THEN NULL
                ELSE description
            END,
            updated_utc = v_now_utc
        WHERE id = p_computer_id;
    END IF;

    IF p_reset_scoped_preferences = 1 THEN
        DELETE FROM config_settings_values
        WHERE computer_id = p_computer_id;
    END IF;

    COMMIT;
END$$

DELIMITER ;
