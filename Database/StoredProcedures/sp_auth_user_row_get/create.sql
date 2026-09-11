-- Stored Procedure: sp_auth_user_row_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: resolve one active user's identity and role at logon. Replaces the inline SELECT in
--          `StartupSessionRepository.ReadUserRowAsync` (FR-015).
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the username as the caller holds it
--
--   OUT zero or one row: id, role_name, display_name, employee_identifier
--
-- Identical to `sp_auth_credentials_check` minus the three credential columns: this read is used for identity and
-- role only, so it never pulls a password hash into memory for a logon that is not checking a password.
--
-- One asymmetry with `sp_auth_credentials_check` is preserved deliberately because it is pre-existing caller
-- behaviour, not a decision taken here: that caller lower-cases the username before the lookup, while this one
-- passes it through unchanged. Both compare against `username_normalized`, so a mixed-case name matches only if
-- the column's collation folds it. Flagged rather than "fixed", because correcting it would change which logons
-- succeed and that is outside this task's scope.
-- ============================================================

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
