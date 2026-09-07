-- Create procedure: sp_mock_locations_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_insert;

CREATE PROCEDURE sp_mock_locations_insert(
    IN p_location_code VARCHAR(50),
    IN p_location_description VARCHAR(255),
    IN p_is_ignored TINYINT
)
INSERT INTO mock_locations (location_code, location_description, is_ignored, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_location_code), ''), NULLIF(TRIM(p_location_description), ''), p_is_ignored, UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

