-- Stored Procedure: sp_core_computers_registry_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
--
-- Purpose: the whole registry, in display order. Replaces the inline SELECT in
--          ComputerRegistryService.GetAllComputersAsync (FR-015).
--
-- Contract:
--   OUT one row per registered machine: id, computer_name, display_name, description,
--       mac_address_normalized, is_registered — ordered by display_name then computer_name, exactly as the
--       statement this replaces ordered them, so the settings list keeps its current sequence.
--
-- No parameters: the caller filters nothing server-side.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_get_all;

CREATE PROCEDURE sp_core_computers_registry_get_all()
SELECT
    id,
    computer_name,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
ORDER BY display_name ASC, computer_name ASC;
