-- Rollback for table: user_active_sessions
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T044)
-- Idempotency: Uses DROP TABLE IF EXISTS for safe re-execution.
--
-- Dropping this table reverses its creation and nothing else. No other table carries a foreign key to it, and
-- it holds no row another table depends on, so the sessions it recorded are the only thing lost. A store rolled
-- back past this point has no session record at all, which is the state it was in before T044.
--
-- The two foreign keys are outgoing, so the tables they point at (`core_users_profiles`,
-- `core_computers_registry`) are untouched.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS user_active_sessions;

SET FOREIGN_KEY_CHECKS = 1;
