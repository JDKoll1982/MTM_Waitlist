-- Stored Procedure: sp_user_management_get
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T010)
--
-- Purpose: one person, for the person's page.
--
-- Contract:
--   IN  p_user_id   BIGINT   the account to read
--
--   OUT zero or one row:
--     user_id, public_id, username_normalized, first_name, last_name, display_name, employee_identifier,
--     role_code, role_name, role_rank, is_active, temporary_credential_failed_attempts
--
-- Reading is never refused (FR-027). This procedure takes no actor and applies no rank rule, so an account
-- that outranks the reader is still readable; the page shows its fields and actions as unavailable and states
-- the reason in words, which is a presentation decision made from `role_rank` rather than from a refusal here.
--
-- `temporary_credential_failed_attempts` is returned because the person's page is where an administrator
-- explains why a handed-over PIN stopped working. It is a count and nothing more: it is never presented as a
-- record of who tried (FR-043).
--
-- Highest rung wins when a person holds more than one role (FR-017), then the code breaks the tie. The pick is
-- a correlated subquery rather than a window function because the engine baseline is MySQL 5.7.
--
-- Zero rows means no such account, which the caller reports as a not-found state rather than an empty page.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_get;

CREATE PROCEDURE sp_user_management_get(
    IN p_user_id BIGINT
)
SELECT u.id AS user_id,
       u.public_id,
       u.username_normalized,
       u.first_name,
       u.last_name,
       u.display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(r.role_rank, 0) AS role_rank,
       u.is_active,
       u.temporary_credential_failed_attempts
FROM core_users_profiles u
LEFT JOIN auth_roles_assignments ra
    ON ra.user_id = u.id
   AND ra.id = (
        SELECT ra2.id
        FROM auth_roles_assignments ra2
        INNER JOIN auth_roles_catalog r2 ON r2.id = ra2.role_id
        WHERE ra2.user_id = u.id
        ORDER BY r2.role_rank DESC, r2.role_code ASC
        LIMIT 1
   )
LEFT JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.id = p_user_id
LIMIT 1;
