-- Rollback: sp_core_computers_registry_upsert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Drops the upsert added by create.sql. Rollback drops the routine only — it deliberately does not undo rows
-- the procedure inserted, because those rows are registry records the application still needs; deleting them
-- would be a data loss dressed up as a code rollback.
--
-- The caller's former inline INSERT ... ON DUPLICATE KEY UPDATE is not restored: if this is dropped while
-- `ComputerRegistryService` still calls it, logon registration fails loudly rather than silently reverting to
-- embedded SQL, which is the correct failure for a constitution-III regression.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_upsert;
