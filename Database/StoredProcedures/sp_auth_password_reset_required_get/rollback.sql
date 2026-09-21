-- Rollback: sp_auth_password_reset_required_get
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T008)
--
-- Restores the previous definition rather than dropping the procedure: this read is on the sign-in path, so an
-- absent procedure breaks the forced-password-change prompt. The reversal removes the `role_code` column this
-- feature added and puts back the lower-case sign-in-name contract comment.
--
-- It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_password_reset_required_get;

CREATE PROCEDURE sp_auth_password_reset_required_get(
    IN p_username VARCHAR(128)
)
SELECT u.id AS user_id,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       CASE
           WHEN u.require_password_change = 1 THEN 1
           WHEN COALESCE(u.password_hash, '') = '' THEN 1
           WHEN u.password_hash = '0000' THEN 1
           ELSE 0
       END AS password_reset_required
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;
