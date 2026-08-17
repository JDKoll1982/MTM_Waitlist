-- Stored Procedure: sp_setup_job_history_scrap_type_get
-- Engine: MySQL 5.7
-- Purpose: Get saved scrap type for an exact part/sequence pair from custom setup data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_scrap_type_get;

CREATE PROCEDURE sp_setup_job_history_scrap_type_get(
    IN p_work_order VARCHAR(32),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32)
)
SELECT selected_scrap_type
FROM setup_part_sequence_custom_data
WHERE part_number = TRIM(p_part_number)
  AND sequence_number = TRIM(p_sequence_number)
ORDER BY updated_utc DESC
LIMIT 1;
