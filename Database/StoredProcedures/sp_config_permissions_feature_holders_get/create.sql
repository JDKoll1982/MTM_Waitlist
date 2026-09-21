-- Stored Procedure: sp_config_permissions_feature_holders_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T054)
--
-- Purpose: answer "who holds this feature", reading only the people who DIFFER from their role's baseline, so the
--          second view on the permissions page reads the few people who were chosen for rather than every person
--          in the store (FR-076).
--
-- Contract:
--   IN  p_setting_key VARCHAR(190)  a key in the permission namespace
--
--   OUT zero or more rows: user_id, display_name, employee_identifier, role_code, is_active, setting_value_bool
--
-- Read-only: nothing here writes, and there is no counterpart write. The view it serves changes nothing, and
-- every change still happens on the person's own page (FR-077).
--
-- What it does NOT return is as important as what it does: the roles whose BASELINE gives the key are deliberately
-- absent. Those are read from the seeded baselines, which the declaration's two-direction check ties to the
-- declaration (FR-046, FR-047, FR-048), and restating them here would be a second copy of the answers that could
-- drift from the first. This procedure answers about people; the baselines answer about roles.
--
-- A person is listed when their own stored value differs from their role's baseline in EITHER direction, so a
-- person whose access was taken away is named exactly as one who was given more (FR-076). A person holding the
-- value but switched off is listed and carries `is_active` so the view can mark them (FR-079).
--
-- The person is matched on `scope_key` rather than on the `user_id` column. That is the keying the data model
-- pins for a person-scoped row, and the same choice sp_config_permissions_user_get documents: a row written with
-- the right `scope_key` and no `user_id` is invisible to a `user_id`-keyed join. That was a real failure seen by
-- running it.
--
-- A person whose role holds NO baseline row for the key is listed when they hold a stored value, because they
-- differ from what their role supplies. Their role supplies the shipped fallback in that case, which lives in the
-- declaration rather than in the store, so this read cannot compare against it; the shipped baselines cover every
-- role the catalogue holds, which is what makes that case theoretical rather than ordinary.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_feature_holders_get;

CREATE PROCEDURE sp_config_permissions_feature_holders_get(
    IN p_setting_key VARCHAR(190)
)
SELECT u.id AS user_id,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(hr.role_code, '') AS role_code,
       u.is_active,
       ov.setting_value_bool
FROM config_settings_values ov
INNER JOIN core_users_profiles u
        ON u.id = CAST(SUBSTRING(ov.scope_key, LENGTH('user:') + 1) AS UNSIGNED)
LEFT JOIN (
    SELECT ra.user_id,
           r.role_code,
           r.role_rank
    FROM auth_roles_assignments ra
    INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
) hr ON hr.user_id = u.id
     AND hr.role_rank = (
         SELECT MAX(r2.role_rank)
         FROM auth_roles_assignments ra2
         INNER JOIN auth_roles_catalog r2 ON r2.id = ra2.role_id
         WHERE ra2.user_id = u.id
     )
LEFT JOIN config_settings_values rv
       ON rv.setting_key = ov.setting_key
      AND rv.scope_type = 'role'
      AND rv.scope_key = CONCAT('role:', COALESCE(hr.role_code, ''))
      AND rv.value_type = 'bool'
WHERE ov.setting_key = p_setting_key
  AND ov.scope_type = 'user'
  AND ov.scope_key LIKE 'user:%'
  AND ov.value_type = 'bool'
  AND (rv.setting_value_bool IS NULL OR IFNULL(ov.setting_value_bool, -1) <> IFNULL(rv.setting_value_bool, -1))
ORDER BY u.is_active DESC, u.display_name ASC;
