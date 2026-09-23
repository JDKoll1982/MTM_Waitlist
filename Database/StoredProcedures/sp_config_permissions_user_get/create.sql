-- Stored Procedure: sp_config_permissions_user_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T014)
--
-- Purpose: the permission rows that are stored for one person and for their role, so the page can show which
--          of them were chosen for the person and which are inherited from the role.
--
-- Contract:
--   IN  p_user_id   BIGINT   the person
--
--   OUT zero or more rows: setting_key, scope_type, scope_key, setting_value_bool, updated_utc
--
-- What it returns is what is STORED, not what is in force. The caller composes the answer: a key with a
-- `user` row takes it, a key with only a `role` row inherits it, and a key with neither falls back to the
-- shipped fallback. That composition is FR-049's order, and keeping it out of the store is what lets the same
-- declaration serve the page, the gate and the who-holds-this view.
--
-- The role is resolved INSIDE this procedure from the person's own assignment, highest rung first, so no caller
-- can ask about a role that is not the person's (FR-049, FR-017).
--
-- Only keys in the `permission.` namespace are returned. A person's rows in this table also carry preferences
-- that are not permissions — the saved roster filter, for one — and those are not part of this answer. Filtering
-- here rather than in the caller keeps the namespace rule in one place, the same rule
-- sp_config_permissions_user_set refuses to break on the way in.
--
-- Both branches match on `scope_key` — `user:<id>` and `role:<role_code>` — rather than on the `user_id` column.
-- `scope_key` is the keying the data model pins ("the person permission value is a `user`-scoped row in the same
-- table, `scope_key` `user:<id>`"), so this read does not depend on a second column being populated by whoever
-- writes the row. That was a real failure seen by running it: a row written with the right `scope_key` and no
-- `user_id` was invisible to the `user_id`-keyed form.
--
-- Users, not roles: this read never answers "who holds this", which is
-- sp_config_permissions_feature_holders_get's job.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_user_get;

-- DELIMITER because this body is compound; see the note in
-- sp_auth_temporary_credential_attempt_record/create.sql.
DELIMITER $$

CREATE PROCEDURE sp_config_permissions_user_get(
    IN p_user_id BIGINT
)
BEGIN
    DECLARE v_role_code VARCHAR(64) DEFAULT '';

    SELECT COALESCE(r.role_code, '')
    INTO v_role_code
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
    WHERE ra.user_id = p_user_id
    ORDER BY r.role_rank DESC, r.role_code ASC
    LIMIT 1;

    SELECT v.setting_key,
           v.scope_type,
           v.scope_key,
           v.setting_value_bool,
           v.updated_utc
    FROM config_settings_values v
    WHERE v.setting_key LIKE 'permission.%'
      AND (
            (v.scope_type = 'user' AND v.scope_key = CONCAT('user:', p_user_id))
            OR (v.scope_type = 'role' AND v.scope_key = CONCAT('role:', v_role_code))
      )
    ORDER BY v.setting_key ASC, v.scope_type ASC;
END$$

DELIMITER ;
