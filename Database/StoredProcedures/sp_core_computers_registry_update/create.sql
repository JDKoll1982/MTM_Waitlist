-- Stored Procedure: sp_core_computers_registry_update
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: edit one registry row by primary key, including its registration flag. Replaces the inline UPDATE in
--          ComputerRegistryService.UpdateComputerAsync (FR-015).
--
-- Contract:
--   IN  p_id                       BIGINT
--   IN  p_computer_name            VARCHAR(128)
--   IN  p_hostname_normalized      VARCHAR(255)
--   IN  p_mac_address_normalized   VARCHAR(64)
--   IN  p_display_name             VARCHAR(128)
--   IN  p_description              VARCHAR(255)   NULL when the caller has no description
--   IN  p_is_registered            TINYINT(1)     0 or 1 — an operator may retire a machine without deleting it
--
-- This is the one path where the MAC *is* rewritten (an operator correcting a mistyped address), which is why
-- it is keyed on `id` rather than on the MAC the way update-by-mac is. `public_id` and `created_utc` stay put.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_update;

CREATE PROCEDURE sp_core_computers_registry_update(
    IN p_id BIGINT,
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255),
    IN p_is_registered TINYINT
)
UPDATE core_computers_registry
SET computer_name = p_computer_name,
    hostname_normalized = p_hostname_normalized,
    mac_address_normalized = p_mac_address_normalized,
    display_name = p_display_name,
    description = p_description,
    is_registered = p_is_registered,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;
