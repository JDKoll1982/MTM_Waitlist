-- Create procedure: sp_mock_work_centers_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_get;

CREATE PROCEDURE sp_mock_work_centers_get()
SELECT id, public_id, work_center_code, work_center_description, building, is_active, created_utc, updated_utc
FROM mock_work_centers
ORDER BY id ASC;

