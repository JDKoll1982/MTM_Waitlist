-- Create procedure: sp_mock_work_orders_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_delete;

CREATE PROCEDURE sp_mock_work_orders_delete(IN p_id BIGINT)
DELETE FROM mock_work_orders WHERE id = p_id;

