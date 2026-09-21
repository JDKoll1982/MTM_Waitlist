-- Stored Procedure: sp_auth_roles_list
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T028)
--
-- Purpose: the one read behind the role picker, the ladder and the rank comparison.
--
-- Contract:
--   IN  none
--
--   OUT zero or more rows, ordered by rung descending then code ascending:
--     role_id, role_code, role_name, role_rank
--
-- This is the only read of the role catalogue in the application. Every rung the ladder compares comes from a row
-- this procedure returns, so the rank comparison and the roles-at-or-above list cannot disagree with each other
-- and there is no second place a rung is written down (FR-010).
--
-- The retired `admin` code is excluded. The catalogue row still exists until the rename seed removes it, and a
-- picker that offered it would offer a role nothing can be assigned by the time this feature ships. Filtering on
-- the code rather than on a rank keeps the read correct whether the retired row carries the column default or a
-- value: the exclusion is about the code, not about the number beside it.
--
-- Ordering is `role_rank DESC, role_code ASC`. The code breaks a tie so peers at one rung (the three worker
-- roles) come back in a stable order rather than in whatever order the engine chooses.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_roles_list;

CREATE PROCEDURE sp_auth_roles_list()
SELECT r.id AS role_id,
       r.role_code,
       r.role_name,
       r.role_rank
FROM auth_roles_catalog r
WHERE r.role_code <> 'admin'
ORDER BY r.role_rank DESC, r.role_code ASC;
