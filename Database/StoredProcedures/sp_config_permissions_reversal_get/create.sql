-- Stored Procedure: sp_config_permissions_reversal_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T055)
--
-- Purpose: the recorded history of the most recent permission save for one person, so that save can be reversed
--          as a whole (FR-069, FR-071). This is a read of config_settings_history, which already exists; the
--          reversal adds no store of its own.
--
-- Contract:
--   IN  p_user_id BIGINT  the person whose last save is being reversed
--
--   OUT zero or more rows: setting_key, previous_setting_value_bool, changed_setting_value_bool,
--       baseline_setting_value_bool, changed_utc
--
-- The rows of one save are found by their exact `changed_utc`, which works because
-- sp_config_permissions_user_set reads the clock ONCE per call and stamps every row it writes with that one
-- value. Nothing about this read guesses a window: it takes the newest timestamp recorded against this person's
-- own scope, and returns every row carrying it.
--
-- The scope is `user:<id>` and nothing else. A settings change made through the ordinary settings path for the
-- same person is not a permission change, and reversing one must not reverse the other.
--
-- WHY THE BASELINE IS RETURNED. `previous_setting_value_bool` is NULL for a permission the person had no stored
-- choice about before the save, and the value then in force was their role's baseline. The reversal cannot restore
-- "no row" by deleting one, because this history table names the value row it changed and that row would be gone:
-- the foreign key from config_settings_history.config_setting_id to config_settings_values.id refused it when it
-- was tried. So the reversal writes the value that was in force instead, and it needs the baseline here to know
-- what that value was. What is restored is therefore the value, and the row that carries it is the person's own
-- rather than the role's: the page marks it as a choice for the person rather than as inherited, which is the one
-- observable difference and is recorded rather than hidden.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_reversal_get;

CREATE PROCEDURE sp_config_permissions_reversal_get(
    IN p_user_id BIGINT
)
SELECT h.setting_key,
       h.previous_setting_value_bool,
       h.changed_setting_value_bool,
       (
           SELECT rv.setting_value_bool
           FROM config_settings_values rv
           WHERE rv.setting_key = h.setting_key
             AND rv.scope_type = 'role'
             AND rv.scope_key = CONCAT(
                     'role:',
                     COALESCE((
                         SELECT r.role_code
                         FROM auth_roles_assignments ra
                         INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
                         WHERE ra.user_id = p_user_id
                         ORDER BY r.role_rank DESC, r.role_code ASC
                         LIMIT 1
                     ), '')
                 )
             AND rv.value_type = 'bool'
           LIMIT 1
       ) AS baseline_setting_value_bool,
       h.changed_utc
FROM config_settings_history h
WHERE h.scope_key = CONCAT('user:', p_user_id)
  AND h.setting_key LIKE 'permission.%'
  AND h.changed_utc = (
        SELECT MAX(h2.changed_utc)
        FROM config_settings_history h2
        WHERE h2.scope_key = CONCAT('user:', p_user_id)
          AND h2.setting_key LIKE 'permission.%'
      )
ORDER BY h.id ASC;
