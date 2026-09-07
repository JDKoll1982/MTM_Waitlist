-- Create procedure: sp_mock_parts_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_insert;

CREATE PROCEDURE sp_mock_parts_insert(
    IN p_part_number VARCHAR(50),
    IN p_part_category VARCHAR(32),
    IN p_part_description VARCHAR(500),
    IN p_unit_of_measure VARCHAR(20)
)
INSERT INTO mock_parts (part_number, part_category, part_description, unit_of_measure, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_part_number), ''), NULLIF(TRIM(p_part_category), ''), NULLIF(TRIM(p_part_description), ''), NULLIF(TRIM(p_unit_of_measure), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

