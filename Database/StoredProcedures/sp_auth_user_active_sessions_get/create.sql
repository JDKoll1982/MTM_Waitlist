-- Stored Procedure: sp_auth_user_active_sessions_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
--
-- Purpose: validate a presented token against the one session stored for a person on a machine, and report it.
--
-- Contract:
--   IN  p_user_id      BIGINT
--   IN  p_computer_id  BIGINT
--   IN  p_token_hash   CHAR(64)  the caller's SHA-256 hex digest
--   OUT zero or one row: public_id, issued_utc, expires_utc, source_label, is_valid
--
-- No row means nothing has ever been stored for this pair and the answer is "not signed in". A row with
-- `is_valid = 0` means a session exists and must not be honoured: it has been cleared, or it has expired.
-- The two are distinct answers on purpose, because a caller that cannot tell them apart cannot report why the
-- person was asked to sign in again.
--
-- The comparison is made by the store: `fn_server_utc_now() < expires_utc` is evaluated here, against the
-- store's clock, exactly as the ruleset requires. Feeding the workstation's time into this decision is the one
-- thing the requirement names and forbids.
--
-- A digest that does not match returns no row rather than an invalid row, so a wrong token cannot be used to
-- learn that a session exists for the pair.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_active_sessions_get;

CREATE PROCEDURE sp_auth_user_active_sessions_get(
    IN p_user_id BIGINT,
    IN p_computer_id BIGINT,
    IN p_token_hash CHAR(64)
)
SELECT
    s.public_id,
    s.issued_utc,
    s.expires_utc,
    s.source_label,
    CASE
        WHEN s.is_active = 1
            AND s.revoked_utc IS NULL
            AND fn_server_utc_now() < s.expires_utc THEN 1
        ELSE 0
    END AS is_valid
FROM
    user_active_sessions s
WHERE
    s.user_id = p_user_id
    AND s.computer_id = p_computer_id
    AND s.token_hash = LOWER(TRIM(p_token_hash))
LIMIT 1;
