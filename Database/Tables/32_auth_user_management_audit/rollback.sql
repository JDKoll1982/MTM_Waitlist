-- Rollback: auth_user_management_audit
-- Engine: MySQL 5.7
-- Reverses: Database/Tables/32_auth_user_management_audit/create.sql
--
-- This is a hard drop. The table is an append-only record of account changes and holds no state any other
-- artifact reads, so there is nothing to restore into it. Dropping it loses the history it holds; if that
-- history matters, export it before running this.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS auth_user_management_audit;

SET FOREIGN_KEY_CHECKS = 1;
