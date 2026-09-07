-- Create procedure: sp_mock_parts_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_parts_get;

CREATE PROCEDURE sp_mock_parts_get()
SELECT id, public_id, part_number, part_category, part_description, unit_of_measure, is_active, created_utc, updated_utc
FROM mock_parts
ORDER BY id ASC;

