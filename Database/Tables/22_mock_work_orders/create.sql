-- Create table: mock_work_orders
-- Engine: MySQL 5.7
-- Purpose: Mock work-order/job master used to build New-Request mock data. Captures the active
--          job on a work center, whether it is coil-bearing, and the part it runs.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_work_orders (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    work_order_number VARCHAR(64) NOT NULL COMMENT 'Work order / job number (e.g. WO-#######).',
    part_number VARCHAR(50) NULL COMMENT 'Part number the job runs (references mock_parts.part_number).',
    work_center_code VARCHAR(32) NULL COMMENT 'Work center the job runs on.',
    is_coil_bearing TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the job has a coil on it.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock work order is active.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_work_orders_public_id (public_id),
    UNIQUE KEY uq_mock_work_orders_work_order_number (work_order_number),
    KEY idx_mock_work_orders_work_center_code (work_center_code)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
