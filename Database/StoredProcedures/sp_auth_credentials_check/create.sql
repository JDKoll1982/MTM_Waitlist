-- Stored Procedure: sp_auth_credentials_check
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093); extended by
--          006-user-management-and-permissions (task T008)
--
-- Purpose: fetch the credential material for one active user, so the caller can verify a supplied password.
--          Replaces the inline SELECT in `StartupSessionRepository.CheckCredentialsAsync` (FR-015).
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the *normalized* (UPPER-case) username; column width from
--                                  core_users_profiles.username_normalized
--
--   OUT zero or one row:
--     id, role_code, role_name, password_hash, password_salt, require_password_change, display_name,
--     employee_identifier, temporary_credential_failed_attempts
--
-- Sign-in-name contract (corrected by T008): a sign-in name is stored and compared in UPPER case, so the
-- caller normalises to upper case before calling. The comment here previously said lower case; that was true
-- before this feature and is not any more (FR-002).
--
-- Semantics preserved exactly from the statement this replaces:
--   * `is_active = 1` only — a deactivated account cannot authenticate.
--   * the role comes from a LEFT JOIN, so a user with no assignment still returns a row with an empty role
--     rather than being treated as unknown;
--   * `ORDER BY ra.assigned_utc DESC LIMIT 1` — the newest assignment wins when a user holds several;
--   * `COALESCE(..., '')` on role/display name/employee id, so the caller never sees NULL for them.
--
-- Added by this feature, and read by ordinal, so the calling code's ordinals moved in the same change:
--   * `role_code` — the identity a gate compares. `role_name` is presentation and is never compared for
--     access (FR-054, FR-105).
--   * `temporary_credential_failed_attempts` — the wrong-attempt count against a temporary credential. It is
--     held with the account so it survives a restart (FR-037) and does not expire with time (FR-038).
--
-- The caller normalizes the username before calling (it applies `Trim().ToUpperInvariant()`); the procedure
-- does not repeat that, so a caller that passes a mixed-case name is not silently rescued into matching. That
-- asymmetry with `sp_auth_user_row_get` is pre-existing caller behaviour and was deliberately preserved.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_credentials_check;

CREATE PROCEDURE sp_auth_credentials_check(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       u.password_hash,
       u.password_salt,
       u.require_password_change,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       u.temporary_credential_failed_attempts
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;
