-- Stored Procedure: sp_core_computers_registry_display_name_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
--
-- Purpose: answer which machine already holds a display name, so a save can ask for a different one instead of
--          colliding on the store's unique key, and a reset can refuse before it writes anything.
--
-- Contract:
--   IN  p_display_name  VARCHAR(128)   the name being claimed (column width from core_computers_registry)
--   OUT zero or one row: id, computer_name, display_name, is_registered
--
-- At most one row can be returned, and that is a fact about the schema rather than about this read:
-- `uq_core_computers_registry_display_name` makes the column unique, so the `LIMIT 1` is there to keep the answer
-- unambiguous rather than to break a tie.
--
-- The read is deliberately keyed on the name and not on the machine. A caller asking "is this name free" knows
-- the name it wants but may not yet have a row at all — a first save creates one — and comparing against the
-- machine it thinks it is would answer the wrong question: the interesting case is precisely the name belonging
-- to a *different* machine, which is what a machine-scoped comparison would miss.
--
-- `id` is returned so the caller can tell "the name is mine already" (rewriting the same name onto the same
-- machine, which is not a collision) from "the name is somebody else's" (a refusal).
--
-- This is a read and returns a result set. It owns no data, so its rollback drops nothing but the routine.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_display_name_get;

CREATE PROCEDURE sp_core_computers_registry_display_name_get(
    IN p_display_name VARCHAR(128)
)
SELECT
    id,
    computer_name,
    display_name,
    is_registered
FROM core_computers_registry
WHERE display_name = p_display_name
LIMIT 1;
