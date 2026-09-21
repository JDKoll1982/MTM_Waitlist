-- Rollback: sp_auth_user_row_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093); reversal of
--          006-user-management-and-permissions (task T008)
--
-- Restores the previous definition rather than dropping the procedure: this read is on the sign-in path, so an
-- absent procedure breaks every sign-in. The reversal removes the `role_code` column this feature added.
--
-- It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_row_get;

CREATE PROCEDURE sp_auth_user_row_get(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;
