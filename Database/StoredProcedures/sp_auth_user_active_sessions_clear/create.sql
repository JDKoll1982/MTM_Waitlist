-- Stored Procedure: sp_auth_user_active_sessions_clear
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
--
-- Purpose: end the session for a person on this machine before the application restarts.
--
-- Contract:
--   IN  p_user_id      BIGINT
--   IN  p_computer_id  BIGINT
--   OUT the affected-row count, through the non-query seam.
--
-- The row is cleared in place and never deleted: `revoked_utc` is stamped from the store clock and `is_active`
-- goes to 0, so the row survives as the record that the session existed and when it ended. That is also why
-- this procedure is safe to run twice — the second run matches nothing, because the row it would match is no
-- longer active, and reports 0.
--
-- The token digest is deliberately left in place. Blanking it would erase the evidence of which token was
-- cleared, and the row is already unusable: every reader requires `is_active = 1` and a null `revoked_utc`.
--
-- There is deliberately no "clear every session for a person" member. A password reset must not end an
-- existing session (S14), so no procedure here clears by person alone.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_active_sessions_clear;

CREATE PROCEDURE sp_auth_user_active_sessions_clear(
    IN p_user_id BIGINT,
    IN p_computer_id BIGINT
)
UPDATE user_active_sessions
SET
    revoked_utc = fn_server_utc_now(),
    is_active = 0
WHERE
    user_id = p_user_id
    AND computer_id = p_computer_id
    AND is_active = 1;
