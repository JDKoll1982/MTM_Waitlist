-- Create procedure: sp_mock_work_orders_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_work_orders_get;

CREATE PROCEDURE sp_mock_work_orders_get()
SELECT id, public_id, work_order_number, part_number, work_center_code, is_coil_bearing, is_active, created_utc, updated_utc
FROM mock_work_orders
ORDER BY id ASC;

