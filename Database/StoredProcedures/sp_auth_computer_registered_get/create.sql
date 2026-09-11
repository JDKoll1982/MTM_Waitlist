-- Stored Procedure: sp_auth_computer_registered_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T093)
--
-- Purpose: answer "is this machine a registered workstation?" for the logon screen. Replaces the inline
--          `SELECT COUNT(1) ...` in `StartupSessionRepository.ReadComputerRegisteredAsync` (FR-015).
--
-- Contract:
--   IN  p_hostname_normalized       VARCHAR(255)   column width from core_computers_registry
--   IN  p_mac_address_normalized    VARCHAR(64)
--
--   OUT one row / one column: the number of matching registered rows (0 when there is none). The caller reads it
--       as a scalar and treats "greater than zero" as registered — the same shape the statement returned.
--
-- Both columns must match: a machine that presents a different MAC than the one it registered with is not the
-- registered machine. `is_registered = 1` is part of the test, so a retired machine answers "no" without being
-- deleted from the registry (which would lose its history).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_computer_registered_get;

CREATE PROCEDURE sp_auth_computer_registered_get(
    IN p_hostname_normalized VARCHAR(255),
    IN p_mac_address_normalized VARCHAR(64)
)
SELECT COUNT(1) AS registered_count
FROM core_computers_registry
WHERE hostname_normalized = p_hostname_normalized
  AND mac_address_normalized = p_mac_address_normalized
  AND is_registered = 1;
