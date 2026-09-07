-- Create procedure: sp_mock_requesters_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_delete;

CREATE PROCEDURE sp_mock_requesters_delete(IN p_id BIGINT)
DELETE FROM mock_requesters WHERE id = p_id;

