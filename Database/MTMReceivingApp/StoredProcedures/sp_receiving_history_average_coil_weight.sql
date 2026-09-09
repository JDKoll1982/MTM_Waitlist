-- ========================================
-- Stored Procedure: sp_receiving_history_average_coil_weight
-- Target database: mtm_receiving_application
-- Purpose: Return the average coil/skid weight (lb) for a part from
--          receiving_history, used by the Waitlist "average coil weight"
--          card line and Pickup Coil resolution.
--
-- Average coil weight = ROUND(AVG(quantity), 0) across every receiving
-- history row whose part_id matches the coil. Each row is one RECEIVED
-- SKID; `quantity` is the skid weight the material handler grasps. Skids
-- may hold 2+ coils, so we average the whole skid weight (we do NOT divide
-- by coils_on_skid). Rows with NULL or non-positive quantity are ignored.
--
-- Engine: MySQL 5.7 / 8+ (mtm_receiving_application).
-- Naming per MTM_Waitlist Database rules (lowercase snake_case) for the
-- MTM_Waitlist-owned read path. Deploy target: mtm_receiving_application.
-- ========================================

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
