-- Stored Procedure: sp_auth_user_password_set
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T116)
--
-- Purpose: replace one account's password with a new one it has just chosen, and leave the temporary state it
--          was in, in one act.
--
-- Contract:
--   IN  p_user_id       BIGINT
--   IN  p_password_hash VARCHAR(128)  the salted hash of the new password, computed by the caller
--   IN  p_password_salt VARBINARY(32) the salt that hash was computed with
--
--   OUT the affected-row count, through the non-query seam. 0 means no such account, which is reported rather
--       than treated as a silent success.
--
-- Why this exists: FR-013 requires a person still on a temporary credential to set a new password before
-- anything else, and `sp_auth_user_password_update` was retired with the rest of the startup-only procedures
-- (T020). Nothing that survives writes this column pair for the account itself: `sp_user_management_update`
-- deliberately leaves credentials alone, and `sp_user_management_reset_password` issues a fresh one-time PIN
-- for an administrator to hand over, which is a different act and would set `require_password_change` back to 1
-- rather than clearing it.
--
-- What it writes, and nothing else:
--   * `password_hash` and `password_salt`, which together are the whole of the credential;
--   * `require_password_change = 0`, because the person has just replaced the credential the flag stood for.
--     This is what takes the account out of the temporary state the five-attempt limit applies to (FR-012);
--   * `temporary_credential_failed_attempts = 0`, because the count is judged against a state the account has
--     just left, and carrying it forward would refuse a fresh password on a later sign-in.
--
-- It deliberately does not touch `is_active`, `display_name`, the role assignment or any session: a person
-- changing their own password does not change who they are, whether they may sign in, or where they are already
-- signed in. It writes no audit row, because the account has no actor to name and the store's audit table is the
-- user-management screen's record of what an administrator did to somebody else.
--
-- The plaintext never arrives. The caller computes the hash and the salt and passes those, so nothing
-- recoverable is ever sent to, held by, or logged by the store.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_password_set;

CREATE PROCEDURE sp_auth_user_password_set(
    IN p_user_id BIGINT,
    IN p_password_hash VARCHAR(128),
    IN p_password_salt VARBINARY(32)
)
UPDATE core_users_profiles
SET
    password_hash = TRIM(p_password_hash),
    password_salt = p_password_salt,
    require_password_change = 0,
    temporary_credential_failed_attempts = 0
WHERE
    id = p_user_id
    AND is_active = 1;
