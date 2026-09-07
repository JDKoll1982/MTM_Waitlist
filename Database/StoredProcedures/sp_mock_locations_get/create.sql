-- Create procedure: sp_mock_locations_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_get;

CREATE PROCEDURE sp_mock_locations_get()
SELECT id, public_id, location_code, location_description, is_ignored, is_active, created_utc, updated_utc
FROM mock_locations
ORDER BY id ASC;

