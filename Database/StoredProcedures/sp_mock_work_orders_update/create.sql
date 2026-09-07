-- Create procedure: sp_mock_work_orders_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_update;

CREATE PROCEDURE sp_mock_work_orders_update(
    IN p_id BIGINT,
    IN p_work_order_number VARCHAR(64),
    IN p_part_number VARCHAR(50),
    IN p_work_center_code VARCHAR(32),
    IN p_is_coil_bearing TINYINT
)
UPDATE mock_work_orders
SET work_order_number = NULLIF(TRIM(p_work_order_number), ''), part_number = NULLIF(TRIM(p_part_number), ''), work_center_code = NULLIF(TRIM(p_work_center_code), ''), is_coil_bearing = p_is_coil_bearing, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

