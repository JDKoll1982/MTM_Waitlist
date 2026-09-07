-- Create procedure: sp_mock_work_orders_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_insert;

CREATE PROCEDURE sp_mock_work_orders_insert(
    IN p_work_order_number VARCHAR(64),
    IN p_part_number VARCHAR(50),
    IN p_work_center_code VARCHAR(32),
    IN p_is_coil_bearing TINYINT
)
INSERT INTO mock_work_orders (work_order_number, part_number, work_center_code, is_coil_bearing, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_work_order_number), ''), NULLIF(TRIM(p_part_number), ''), NULLIF(TRIM(p_work_center_code), ''), p_is_coil_bearing, UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

