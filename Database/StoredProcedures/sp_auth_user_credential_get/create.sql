-- Stored Procedure: sp_auth_user_credential_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T110)
--
-- Purpose: return one account's identity and the material that confirms its credential, keyed on the
--          upper-normalised sign-in name, so a caller can check a credential without a second read of the
--          same row.
--
-- Contract:
--   IN  p_username  VARCHAR(128)  the sign-in name as the caller holds it, stored upper case
--
--   OUT zero or one row: id, role_code, display_name, employee_identifier, password_hash, password_salt,
--       require_password_change, temporary_credential_failed_attempts
--
-- Why this exists: `sp_auth_credentials_check` was retired with the rest of the startup-only procedures
-- (T020), and nothing that survives returns a credential column. `sp_auth_user_row_get` deliberately omits
-- them, so an identity read never pulls a hash into memory. Two callers need the credential material, and
-- they are asking the same question, so they share one read instead of each inventing one: the sign-in
-- path's credential check, and the machine-setup gate that authenticates the person who configures a
-- computer.
--
-- It mirrors `sp_auth_user_row_get` on purpose, with the same join, the same active-row filter and the same
-- `ORDER BY ra.assigned_utc DESC LIMIT 1`, so the two reads cannot disagree about which row is the person or
-- which role is in force. What it adds is the four columns that read leaves out.
--
-- Two credential-state columns travel with the identity because one attempt must cost one read.
-- `require_password_change` is what makes an account "still on a temporary credential" rather than "on an
-- ordinary one", and the five-attempt limit applies to the temporary state alone (FR-012).
-- `temporary_credential_failed_attempts` is the count that limit is judged against, held with the account so
-- it survives closing and reopening the application (FR-012, SC-007).
--
-- `role_code` is the identity a gate compares and `role_name` is presentation, so only the code is returned.
--
-- The read owns no data and writes nothing. It never returns the plaintext of anything: `password_hash` is a
-- PBKDF2 digest and `password_salt` is the salt it was computed with, which is all a comparison needs.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_credential_get;

CREATE PROCEDURE sp_auth_user_credential_get(
    IN p_username VARCHAR(128)
)
SELECT u.id,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(u.display_name, '') AS display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(u.password_hash, '') AS password_hash,
       u.password_salt,
       u.require_password_change,
       u.temporary_credential_failed_attempts
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra ON ra.user_id = u.id
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC
LIMIT 1;
