-- Stored Procedure: sp_auth_user_row_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093); extended by
--          006-user-management-and-permissions (task T008)
--
-- Purpose: resolve one active user's identity and role at logon. Replaces the inline SELECT in
--          `StartupSessionRepository.ReadUserRowAsync` (FR-015).
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the username as the caller holds it
--
--   OUT zero or one row: id, role_code, role_name, display_name, employee_identifier
--
-- Identical to `sp_auth_credentials_check` minus the credential columns: this read is used for identity and
-- role only, so it never pulls a password hash into memory for a logon that is not checking a password.
--
-- `role_code` is the identity a gate compares; `role_name` is presentation and is never compared for access
-- (FR-054, FR-105). Both callers of this read need the code.
--
-- Sign-in-name contract (corrected by T008): a sign-in name is stored and compared in UPPER case (FR-002). The
-- asymmetry noted here before — that `sp_auth_credentials_check`'s caller normalised and this one's did not —
-- is still pre-existing caller behaviour, but the direction of that normalisation is now upper case.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_row_get;

CREATE PROCEDURE sp_auth_user_row_get(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_code, '') AS role_code,
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
