-- Stored Procedure: sp_core_employee_by_identifier_get
-- Engine: MySQL 5.7
-- Feature: 004-unified-card-item-picker (task T143)
--
-- Purpose: resolve ONE account by the employee identifier the signed-in session carries, so a request can be
--          attributed to the person who actually raised it (FR-046). It replaces a hard-coded identity in the
--          New Request wizard's Work Center step, which named `6229` "John Koll" for every signed-in person and
--          therefore attributed every request in the store to one man.
--
-- Contract:
--   IN  p_employee_identifier  VARCHAR(128)  the identifier the caller holds
--
--   OUT zero or one row: employee_identifier, display_name, is_active
--
-- The comparison is on the STORED IDENTIFIER, never on a name: a name is not the key an account is found by, and
-- two people may hold similar names. An identifier no account carries returns no row at all, which is what lets
-- the caller refuse it instead of inventing a name for it.
--
-- Identifiers are not unique by themselves — `johnk` and `jkoll` deliberately share `6229`, both being John Koll
-- — so the read is ordered to answer with the active account first when more than one carries the identifier.
--
-- The row is returned even when the account is inactive, so the caller can say "that employee is not active"
-- rather than the weaker "no such employee".
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_employee_by_identifier_get;

CREATE PROCEDURE sp_core_employee_by_identifier_get(
    IN p_employee_identifier VARCHAR(128)
)
SELECT u.employee_identifier,
       COALESCE(u.display_name, '') AS display_name,
       u.is_active
FROM core_users_profiles u
WHERE u.employee_identifier IS NOT NULL
  AND u.employee_identifier = p_employee_identifier
ORDER BY u.is_active DESC, u.id ASC
LIMIT 1;
