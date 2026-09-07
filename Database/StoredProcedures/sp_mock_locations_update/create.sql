-- Create procedure: sp_mock_locations_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_update;

CREATE PROCEDURE sp_mock_locations_update(
    IN p_id BIGINT,
    IN p_location_code VARCHAR(50),
    IN p_location_description VARCHAR(255),
    IN p_is_ignored TINYINT
)
UPDATE mock_locations
SET location_code = NULLIF(TRIM(p_location_code), ''), location_description = NULLIF(TRIM(p_location_description), ''), is_ignored = p_is_ignored, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

