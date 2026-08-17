-- Stored Procedure: sp_setup_active_job_dunnage_assignments_get
-- Engine: MySQL 5.7
-- Purpose: Get saved dunnage assignments JSON for an exact part/sequence pair from custom setup data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_active_job_dunnage_assignments_get;

CREATE PROCEDURE sp_setup_active_job_dunnage_assignments_get(
    IN p_work_order VARCHAR(32),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32)
)
SELECT selected_dunnage_parts_json
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
  AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1;
