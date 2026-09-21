-- Stored Procedure: sp_auth_temporary_credential_attempt_record
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T009)
--
-- Purpose: record the outcome of one sign-in attempt made against a temporary credential, so the account's
--          wrong-attempt count goes up on a failure and back to zero on a success.
--
-- Contract:
--   IN  p_user_id          BIGINT   the account the attempt was made against
--   IN  p_was_successful   TINYINT  1 when the attempt succeeded, 0 when it failed
--   OUT the affected-row count, through the non-query seam
--
-- The count is held with the account, which is what makes it survive closing and reopening the application
-- (FR-037) and what makes it not expire with time (FR-038). Nothing here expires it and no other procedure
-- clears it except a fresh reset (sp_user_management_reset_password), so FR-039 has exactly two writers and
-- both are named.
--
-- Only the one column is written. The procedure deliberately does not touch `updated_utc`: this is a
-- credential-state counter, not a profile change, and moving the profile's updated timestamp would make the
-- account look edited when it was not.
--
-- A non-query on purpose, so a caller that cannot tell an unknown account from a recorded attempt can read
-- the affected-row count and decide. An unknown `p_user_id` affects no rows and is not an error.
--
-- FR-043: the count is a state, not a log. Nothing reads it back as a record of who tried.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_temporary_credential_attempt_record;

-- DELIMITER is required here and in every other compound body in this feature. The mysql client splits a script
-- on `;` unless told otherwise, so a BEGIN … END body arrives as fragments and the CREATE fails with 1064. No
-- procedure in this repo needed it before, because every one of them is a single statement.
DELIMITER $$

CREATE PROCEDURE sp_auth_temporary_credential_attempt_record(
    IN p_user_id BIGINT,
    IN p_was_successful TINYINT
)
BEGIN
    IF p_was_successful = 1 THEN
        UPDATE core_users_profiles
        SET temporary_credential_failed_attempts = 0
        WHERE id = p_user_id;
    ELSE
        UPDATE core_users_profiles
        SET temporary_credential_failed_attempts = temporary_credential_failed_attempts + 1
        WHERE id = p_user_id;
    END IF;
END$$

DELIMITER ;
