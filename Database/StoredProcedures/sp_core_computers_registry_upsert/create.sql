-- Stored Procedure: sp_core_computers_registry_upsert
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: register a computer, or refresh the row it already has. Replaces the inline
--          INSERT ... ON DUPLICATE KEY UPDATE in ComputerRegistryService.UpsertComputerAsync (FR-015).
--
-- Contract:
--   IN  p_computer_name            VARCHAR(128)
--   IN  p_hostname_normalized      VARCHAR(255)
--   IN  p_mac_address_normalized   VARCHAR(64)
--   IN  p_display_name             VARCHAR(128)
--   IN  p_description              VARCHAR(255)   NULL when the caller has no description
--
-- Semantics preserved from the statement this replaces:
--   * `public_id` is generated on insert and never rewritten on update (UUID() is only in the VALUES list).
--   * `created_utc` is set on insert and left alone on update.
--   * The duplicate key is whichever unique key collides (display_name is UNIQUE, and the
--     (computer_name, mac_address_normalized) pair is indexed): the update arms are the same ones the inline
--     statement carried — computer_name, display_name, description, is_registered = 1, updated_utc.
--   * `is_registered` is always set back to 1: seeing the machine at logon is what registration means.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_upsert;

CREATE PROCEDURE sp_core_computers_registry_upsert(
    IN p_computer_name VARCHAR(128),
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64),
    IN p_display_name VARCHAR(128),
    IN p_description VARCHAR(255)
)
INSERT INTO core_computers_registry (
    public_id,
    computer_name,
    hostname_normalized,
    mac_address_normalized,
    display_name,
    description,
    is_registered,
    created_utc,
    updated_utc
)
VALUES (
    UUID(),
    p_computer_name,
    p_hostname_normalized,
    p_mac_address_normalized,
    p_display_name,
    p_description,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    computer_name = VALUES(computer_name),
    display_name = VALUES(display_name),
    description = VALUES(description),
    is_registered = 1,
    updated_utc = UTC_TIMESTAMP();
