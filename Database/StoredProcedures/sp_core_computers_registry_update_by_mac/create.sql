-- Stored Procedure: sp_core_computers_registry_update_by_mac
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: follow a machine that was renamed — update the newest row for a MAC address. Replaces the inline
--          UPDATE in ComputerRegistryService.UpdateComputerByMacAsync (FR-015).
--
-- Contract:
--   IN  p_mac_address_normalized   VARCHAR(64)
--   IN  p_computer_name            VARCHAR(128)
--   IN  p_hostname_normalized      VARCHAR(255)
--   IN  p_display_name             VARCHAR(128)
--   IN  p_description              VARCHAR(255)   NULL when the caller has no description
--
-- `ORDER BY id DESC LIMIT 1` is kept deliberately: a MAC can legitimately have more than one row (renames,
-- reimages), and the caller's intent is to move the newest one. `mac_address_normalized` and `is_registered`
-- are not touched — the statement this replaces did not touch them either, and rewriting the MAC here would
-- break the identity the row is being found by.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_update_by_mac;

CREATE PROCEDURE sp_core_computers_registry_update_by_mac(
    IN p_mac_address_normalized VARCHAR(64),
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255)
)
UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    display_name = p_display_name,
    description = p_description,
    updated_utc = UTC_TIMESTAMP()
WHERE mac_address_normalized = p_mac_address_normalized
ORDER BY id DESC
LIMIT 1;
