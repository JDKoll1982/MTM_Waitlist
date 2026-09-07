-- Create procedure: sp_mock_work_centers_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_update;

CREATE PROCEDURE sp_mock_work_centers_update(
    IN p_id BIGINT,
    IN p_work_center_code VARCHAR(32),
    IN p_work_center_description VARCHAR(255),
    IN p_building VARCHAR(128)
)
UPDATE mock_work_centers
SET work_center_code = NULLIF(TRIM(p_work_center_code), ''), work_center_description = NULLIF(TRIM(p_work_center_description), ''), building = NULLIF(TRIM(p_building), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

