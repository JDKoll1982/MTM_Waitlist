-- ============================================================
-- Stored Procedure: sp_auth_password_reset_required_get
-- Engine: MySQL 5.7
-- Purpose: Report whether one account still holds its temporary default password, so the application
--          can put the operator straight onto the "set a new password" surface at startup, BEFORE the
--          sign-in form is ever shown.
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the username as the caller holds it, matched against
--                                  `username_normalized` exactly as `sp_auth_user_row_get` does
--
--   OUT zero or one row:
--       user_id, role_name, display_name, employee_identifier, password_reset_required
--
-- `password_reset_required` is 1 when either of the two signals the sign-in read already honours is
-- present: the stored password is the temporary marker (`'0000'`, or an empty/absent hash — see
-- `IsTemporaryDefaultPassword` in `StartupSessionRepository`), or the row's `require_password_change`
-- flag is set. A user who has set a real password has the flag cleared by
-- `sp_auth_user_password_update`, so this answers "has this account ever been signed in with?".
--
-- No credential material is returned — only the 0/1 verdict — so the read is safe to call before any
-- authentication has happened. The row is the same one `sp_auth_user_row_get` returns, so the caller
-- gets the identity it needs to complete the reset without a second lookup.
--
-- Returns zero rows for an unknown or inactive user, which the caller treats as "no prompt".
-- ============================================================

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
