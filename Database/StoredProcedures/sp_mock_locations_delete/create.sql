-- Create procedure: sp_mock_locations_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_locations_delete;

CREATE PROCEDURE sp_mock_locations_delete(IN p_id BIGINT)
DELETE FROM mock_locations WHERE id = p_id;

