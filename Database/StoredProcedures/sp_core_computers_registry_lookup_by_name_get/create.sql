-- Stored Procedure: sp_core_computers_registry_lookup_by_name_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092; found while routing WorkCenterCatalogService)
--
-- Purpose: resolve one registry row from a machine name *or* its normalized hostname, preferring the exact
--          name match. Replaces two identical inline SELECTs in `WorkCenterCatalogService` —
--          `SaveHotWorkCentersAsync`'s workstation resolve and `ResolveCurrentComputerNameAsync` (FR-015).
--
-- Why this is an extra artifact: task T092 listed a lookup by (name + MAC) and a lookup by MAC, but the shared
-- service has no MAC at hand when it resolves a workstation — it has only the name it was given or the local
-- machine name. Both of its statements therefore matched on either column, with an exact-match tie-break. That
-- shape had no procedure, so this pair was added rather than widening one of the two that exist (a name+MAC
-- procedure cannot answer a name-only question, and a MAC-only one cannot either).
--
-- Contract:
--   IN  p_name  VARCHAR(255)   a computer_name or a hostname_normalized value
--   OUT zero or one row: id, computer_name, hostname_normalized, display_name, description,
--       mac_address_normalized, is_registered
--
--   The row is chosen by `ORDER BY CASE WHEN computer_name = p_name THEN 0 ELSE 1 END ... LIMIT 1`, so a name
--   that matches one machine's computer_name and another's hostname resolves to the computer_name match. That
--   is exactly the ordering the two statements this replaces used; dropping it would make the result depend on
--   insertion order.
--
-- Parameter width is 255 because it must accept a `hostname_normalized` (varchar(255)), which is wider than
-- `computer_name` (varchar(128)).
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_get;

CREATE PROCEDURE sp_core_computers_registry_lookup_by_name_get(
    IN p_name VARCHAR(255)
)
SELECT
    id,
    computer_name,
    hostname_normalized,
    display_name,
    description,
    mac_address_normalized,
    is_registered
FROM core_computers_registry
WHERE computer_name = p_name
   OR hostname_normalized = p_name
ORDER BY CASE WHEN computer_name = p_name THEN 0 ELSE 1 END
LIMIT 1;
