-- Stored Procedure: sp_core_computers_registry_lookup_by_name_mac_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: find the registry row for one exact (computer_name, mac_address_normalized) pair — the identity a
--          logon presents. Replaces the inline SELECT in ComputerRegistryService.LookupComputerAsync (FR-015).
--
-- Contract:
--   IN  p_computer_name           VARCHAR(128)  column width from core_computers_registry
--   IN  p_mac_address_normalized  VARCHAR(64)
--   OUT zero or one row: id, computer_name, display_name, description, mac_address_normalized, is_registered
--
-- No TRIM here: the caller already trims both values, so adding it would only hide a caller that stopped
-- doing so. The predicate is otherwise byte-for-byte the statement this replaces.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_mac_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_name_mac_get(
    IN p_computer_name VARCHAR(128),
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
WHERE computer_name = p_computer_name
  AND mac_address_normalized = p_mac_address_normalized
LIMIT 1;
