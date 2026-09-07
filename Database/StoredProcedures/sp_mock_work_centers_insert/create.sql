-- Create procedure: sp_mock_work_centers_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_insert;

CREATE PROCEDURE sp_mock_work_centers_insert(
    IN p_work_center_code VARCHAR(32),
    IN p_work_center_description VARCHAR(255),
    IN p_building VARCHAR(128)
)
INSERT INTO mock_work_centers (work_center_code, work_center_description, building, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_work_center_code), ''), NULLIF(TRIM(p_work_center_description), ''), NULLIF(TRIM(p_building), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

