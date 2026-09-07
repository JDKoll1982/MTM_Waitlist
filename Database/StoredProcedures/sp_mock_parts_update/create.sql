-- Create procedure: sp_mock_parts_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_update;

CREATE PROCEDURE sp_mock_parts_update(
    IN p_id BIGINT,
    IN p_part_number VARCHAR(50),
    IN p_part_category VARCHAR(32),
    IN p_part_description VARCHAR(500),
    IN p_unit_of_measure VARCHAR(20)
)
UPDATE mock_parts
SET part_number = NULLIF(TRIM(p_part_number), ''), part_category = NULLIF(TRIM(p_part_category), ''), part_description = NULLIF(TRIM(p_part_description), ''), unit_of_measure = NULLIF(TRIM(p_unit_of_measure), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

