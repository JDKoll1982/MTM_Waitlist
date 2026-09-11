-- Stored Procedure: sp_server_utc_now_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: the database server's UTC clock, so session validity is judged against the server rather than the
--          client. Replaces the inline `SELECT fn_server_utc_now();` in
--          `StartupSessionRepository.ReadServerTimeUtcAsync` (FR-015, and the schema ruleset's session rule that
--          "session validity must be comparable to DB server UTC").
--
-- Contract:
--   OUT one row / one column: the server's UTC timestamp, from the existing `fn_server_utc_now()` function.
--
-- The function itself is unchanged and still the single implementation of "now" — this only gives the application
-- a procedure-shaped way to ask for it, so no statement text has to live in C#.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_server_utc_now_get;

CREATE PROCEDURE sp_server_utc_now_get()
SELECT fn_server_utc_now() AS server_utc_now;
