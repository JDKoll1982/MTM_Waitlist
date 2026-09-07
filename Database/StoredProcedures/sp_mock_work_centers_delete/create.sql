-- Create procedure: sp_mock_work_centers_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_centers_delete;

CREATE PROCEDURE sp_mock_work_centers_delete(IN p_id BIGINT)
DELETE FROM mock_work_centers WHERE id = p_id;

