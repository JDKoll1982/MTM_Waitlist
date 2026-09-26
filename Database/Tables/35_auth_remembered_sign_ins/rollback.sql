-- Rollback for table: auth_remembered_sign_ins
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
-- Idempotency: Uses DROP TABLE IF EXISTS for safe re-execution.
--
-- Dropping this table reverses its creation and nothing else. It holds no row another table depends on and no
-- other table carries a foreign key to it. After this drop the machine offers no remembered sign-in and every
-- sign-in is typed, which is the behaviour a store without this table already had.
--
-- The loss is intentional and cheap: the payload is a convenience, never the only way to sign in (FR-014).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS auth_remembered_sign_ins;

SET FOREIGN_KEY_CHECKS = 1;
