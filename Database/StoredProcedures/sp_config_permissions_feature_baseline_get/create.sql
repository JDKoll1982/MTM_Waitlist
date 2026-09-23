-- Stored Procedure: sp_config_permissions_feature_baseline_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T060)
--
-- Purpose: the roles whose baseline gives one feature, read from the seeded baselines (FR-075).
--
-- Contract:
--   IN  p_setting_key VARCHAR(190)  a key in the permission namespace
--
--   OUT zero or more rows: role_code, role_name, setting_value_bool
--
-- WHY THIS READ EXISTS RATHER THAN A SECOND COPY BESIDE THE VIEW. The answer to "which roles' baselines give this"
-- is the seeded data, and the declaration's two-direction check ties the declaration to that same data (FR-046,
-- FR-047, FR-048). Restating the answer beside the view would be a third copy, and a third copy is what drifts.
-- The view therefore asks the store what the baselines say.
--
-- Only `role`-scoped rows are returned. A person's own row is not a baseline, and it is the OTHER query on this
-- page — the one that names the people who differ — that answers about people.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_feature_baseline_get;

CREATE PROCEDURE sp_config_permissions_feature_baseline_get(
    IN p_setting_key VARCHAR(190)
)
SELECT r.role_code,
       r.role_name,
       v.setting_value_bool
FROM config_settings_values v
INNER JOIN auth_roles_catalog r
        ON v.scope_key = CONCAT('role:', r.role_code)
WHERE v.setting_key = p_setting_key
  AND v.scope_type = 'role'
  AND v.value_type = 'bool'
  AND v.setting_value_bool = 1
ORDER BY r.role_rank DESC, r.role_code ASC;
