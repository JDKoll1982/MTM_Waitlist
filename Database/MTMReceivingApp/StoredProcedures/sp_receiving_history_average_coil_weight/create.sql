-- ============================================================
-- Stored Procedure: sp_receiving_history_average_coil_weight
-- Database:   mtm_receiving_application
-- Engine:     MySQL 5.7 (validated live on 5.7.24)
-- Kind:       StoredProcedure / create
-- Created:    2026-09-10
-- Feature:    001-module-mock-visual-fallback (task T088)
-- ============================================================
--
-- Purpose
-- -------
-- Return the average coil/skid weight (lb) for one part from `receiving_history`, for the Waitlist
-- "average coil weight" card line and Pickup Coil resolution (FR-015).
--
-- Why it exists
-- -------------
-- `MTM_Waitlist.Waitlist.View/Services/AverageCoilWeightService.cs` used to carry the statement text
-- inline:
--     SELECT ROUND(AVG(quantity), 0) AS AverageWeight FROM receiving_history WHERE part_id = @partId
-- Constitution III forbids inline statement text in application code (FR-015, SC-013), so the read
-- moved here and the service now calls this procedure through `IMySqlHelperServer`.
--
-- Semantics (unchanged from the statement it replaces, made explicit)
-- -----------------------------------------------------------------
--   Average coil weight = ROUND(AVG(quantity), 0) over every receiving_history row whose part_id
--   matches. Each row is one RECEIVED SKID and `quantity` is the skid weight the material handler
--   grasps; a skid may hold two or more coils, so the whole skid weight is averaged and there is
--   deliberately no division by coils-on-skid. Rows with NULL or non-positive `quantity` are ignored,
--   so an all-invalid part yields no row and the caller renders its empty text — which is exactly what
--   the previous statement returned.
--
-- Contract
-- --------
--   IN  p_part_id  VARCHAR(64)   the part id, matched against TRIM(part_id)
--
--   OUT (zero or one row)
--     AverageWeight   DECIMAL — the rounded average skid weight
--
-- Naming
-- ------
-- Identifiers are lowercase snake_case and the parameter carries the repo's `p_` prefix. The OUT column
-- keeps the name `AverageWeight` the caller already maps, because the previous caller read that key.
--
-- Artifact layout
-- ---------------
-- This file replaces the former flat `sp_receiving_history_average_coil_weight.sql`, which did not
-- follow the locked file-per-artifact layout (`<name>/create.sql` + `<name>/rollback.sql`,
-- `.github/instructions/database-schema-rules.instructions.md`). The receiving tree has no
-- `AllSPs.sql`/`update_table_descriptions.sql` of its own — those belong to the `mtm_waitlist` tree — so
-- this pair is the whole registration.
-- ============================================================

USE mtm_receiving_application;

DROP PROCEDURE IF EXISTS sp_receiving_history_average_coil_weight;

CREATE PROCEDURE sp_receiving_history_average_coil_weight(
    IN p_part_id VARCHAR(64)
)
SELECT ROUND(AVG(quantity), 0) AS AverageWeight
FROM receiving_history
WHERE part_id = TRIM(p_part_id)
  AND quantity IS NOT NULL
  AND quantity > 0;
