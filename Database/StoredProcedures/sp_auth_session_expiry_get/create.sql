-- Stored Procedure: sp_auth_session_expiry_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: report the expiry of a user's current database session, if one exists. Replaces the inline SELECT in
--          `StartupSessionRepository.ReadSessionExpiryUtcAsync` (FR-015).
--
-- Contract:
--   IN  p_user_id  BIGINT
--   OUT zero or one row / one column: `expires_utc` of the newest live session, or no row when the user has none
--       (which the caller reads as "no database session" rather than as an error).
--
-- "Live" means exactly what the statement this replaces meant: `is_active = 1` and `revoked_utc IS NULL`. A
-- revoked or deactivated token must not be reported as a valid session, so neither predicate is dropped.
-- `ORDER BY expires_utc DESC LIMIT 1` keeps the caller's reading: the session that lasts longest wins when more
-- than one is live.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_session_expiry_get;

CREATE PROCEDURE sp_auth_session_expiry_get(
    IN p_user_id BIGINT
)
SELECT expires_utc
FROM auth_sessions_tokens
WHERE user_id = p_user_id
  AND is_active = 1
  AND revoked_utc IS NULL
ORDER BY expires_utc DESC
LIMIT 1;
