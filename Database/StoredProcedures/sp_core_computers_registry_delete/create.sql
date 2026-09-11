-- Stored Procedure: sp_core_computers_registry_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: remove one registry row by primary key. Replaces the inline DELETE in
--          ComputerRegistryService.DeleteComputerAsync (FR-015).
--
-- Contract:
--   IN  p_id BIGINT
--   OUT no result set. The affected-row count reaches the caller from the client (MySqlConnector's non-query
--       return value), and the caller turns it into the boolean it returns: deleting an already-absent row is
--       a successful no-op that reports 0, which is what the inline statement did.
--
-- A hard delete is the documented policy for a primary entity in this schema, and no foreign key references
-- this table, so nothing is orphaned by removing a row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_delete;

CREATE PROCEDURE sp_core_computers_registry_delete(
    IN p_id BIGINT
)
DELETE FROM core_computers_registry
WHERE id = p_id;
