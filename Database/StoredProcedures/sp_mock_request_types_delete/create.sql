-- Create procedure: sp_mock_request_types_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_delete;

CREATE PROCEDURE sp_mock_request_types_delete(IN p_id BIGINT)
DELETE FROM mock_request_types WHERE id = p_id;

