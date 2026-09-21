-- Stored Procedure: sp_user_management_list
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T010)
--
-- Purpose: the roster. Search across the four facts people actually know and filter by role, defaulting to
--          everyone.
--
-- Contract:
--   IN  p_search      VARCHAR(128)   free text, or NULL/empty to match everyone
--   IN  p_role_code   VARCHAR(64)    a role code, or NULL/empty to filter nothing
--
--   OUT one row per person:
--     user_id, public_id, username_normalized, display_name, employee_identifier, role_code, role_name,
--     role_rank, is_active
--
-- Everyone is returned by default and switched-off people are included (FR-088): a deactivated account is
-- still a person the reader needs to find and switch back on.
--
-- Search matches the sign-in name, the display name, the employee number and the role name (FR-086). The
-- comparison is case-insensitive because every column here is utf8mb4_unicode_ci.
--
-- Reading is never refused (FR-027): this procedure takes no actor and applies no rank rule, so an account the
-- reader cannot change is still readable. The unchangeable case is decided where the change is written, not
-- here.
--
-- Where a person holds more than one role, the highest rung decides (FR-017), then the code breaks a tie so the
-- answer is deterministic. The pick is a correlated subquery rather than a window function because this repo's
-- engine baseline is MySQL 5.7, which has no ROW_NUMBER().
--
-- No per-person permission row and no sample row is ever produced here. An unreachable store is reported by the
-- caller as an unavailable state with a retry, never as an empty roster (FR-096).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_user_management_list;

CREATE PROCEDURE sp_user_management_list(
    IN p_search VARCHAR(128),
    IN p_role_code VARCHAR(64)
)
SELECT u.id AS user_id,
       u.public_id,
       u.username_normalized,
       u.display_name,
       COALESCE(u.employee_identifier, '') AS employee_identifier,
       COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(r.role_rank, 0) AS role_rank,
       u.is_active
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
WHERE (
        p_search IS NULL
        OR TRIM(p_search) = ''
        OR u.username_normalized LIKE CONCAT('%', TRIM(p_search), '%')
        OR u.display_name LIKE CONCAT('%', TRIM(p_search), '%')
        OR COALESCE(u.employee_identifier, '') LIKE CONCAT('%', TRIM(p_search), '%')
        OR COALESCE(r.role_name, '') LIKE CONCAT('%', TRIM(p_search), '%')
    )
  AND (
        p_role_code IS NULL
        OR TRIM(p_role_code) = ''
        OR r.role_code = TRIM(p_role_code)
    )
ORDER BY u.display_name ASC, u.username_normalized ASC;
