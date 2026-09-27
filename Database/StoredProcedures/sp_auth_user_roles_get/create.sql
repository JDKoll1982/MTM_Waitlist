-- Stored Procedure: sp_auth_user_roles_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T195)
--
-- Purpose: return every role a named person holds, one row per assignment, so the identity contract's
--          `HeldRoleCodes` is the person's whole set rather than the single role `sp_auth_user_row_get` puts in
--          force. Closes the held-roles gap disclosed in the concerns log.
--
-- Contract:
--   IN  p_username  VARCHAR(128)   the sign-in name as the caller holds it, upper-normalised in the store
--
--   OUT zero or more rows, ordered by the assignment moment descending then code ascending:
--     role_code, role_name, role_rank, assigned_utc
--
-- It is read-only: a read returns rows and takes no OUT parameter. It owns no data, so a reversal cannot lose a
-- row.
--
-- The companion read is `sp_auth_user_row_get`: that one resolves the person and the *one* role in force for the
-- session, and this one returns the *whole* set of assignments. Both are keyed on the same sign-in name, so a
-- caller that needs both reads them together and never has to guess which role the other one would have chosen.
--
-- Ordering is `assigned_utc DESC, role_code ASC`. The order is presentation only — the identity's held set is a
-- set and does not depend on it — but a stable order keeps a test and an inspection report from flickering.
--
-- An inner join on the assignments is deliberate: a person with no assignment holds no role, and the honest
-- answer to that is zero rows rather than a row with a blank code. The retired `admin` code is intentionally NOT
-- filtered here, because `HeldRoleCodes` is defined as "every assignment for the person"; the rename seed moves
-- any surviving `admin` assignment to `it_department`, and the catalogue read `sp_auth_roles_list` is what keeps
-- the retired code out of the picker.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_roles_get;

CREATE PROCEDURE sp_auth_user_roles_get(
    IN p_username VARCHAR(128)
)
SELECT COALESCE(r.role_code, '') AS role_code,
       COALESCE(r.role_name, '') AS role_name,
       COALESCE(r.role_rank, 0) AS role_rank,
       ra.assigned_utc AS assigned_utc
FROM core_users_profiles u
JOIN auth_roles_assignments ra ON ra.user_id = u.id
JOIN auth_roles_catalog r ON r.id = ra.role_id
WHERE u.username_normalized = p_username
  AND u.is_active = 1
ORDER BY ra.assigned_utc DESC, r.role_code ASC;
