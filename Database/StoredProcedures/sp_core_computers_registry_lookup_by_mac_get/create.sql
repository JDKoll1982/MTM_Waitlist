-- Stored Procedure: sp_core_computers_registry_lookup_by_mac_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: find the most recently updated registry row for a MAC address, whatever name it currently holds.
--          Replaces the inline SELECT in ComputerRegistryService.LookupComputerByMacAsync (FR-015).
--
-- Contract:
--   IN  p_mac_address_normalized  VARCHAR(64)
--   OUT zero or one row: id, computer_name, display_name, description, mac_address_normalized, is_registered
--       (newest updated_utc first — the same tie-break the inline statement used, so a machine that was
--        renamed resolves to its current row rather than an older one)
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_mac_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_mac_get(
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE mac_address_normalized = p_mac_address_normalized
ORDER BY updated_utc DESC
LIMIT 1;
