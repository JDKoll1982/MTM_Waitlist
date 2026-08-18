-- Create table: waitlist_requests_queue
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_requests_queue (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building VARCHAR(64) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    request_type VARCHAR(64) NOT NULL,
    subtype VARCHAR(64) NULL,
    input_value VARCHAR(255) NULL,
    active_setup_job_id VARCHAR(64) NOT NULL,
    workstation_name VARCHAR(64) NOT NULL,
    requester_employee_number VARCHAR(32) NOT NULL,
    requester_employee_name VARCHAR(128) NOT NULL,
    status VARCHAR(32) NOT NULL DEFAULT 'Pending',
    requested_utc DATETIME NOT NULL,
    target_time_utc DATETIME NULL,
    is_overdue TINYINT(1) NOT NULL DEFAULT 0,
    assigned_material_handler VARCHAR(128) NULL,
    cancellation_reason VARCHAR(255) NULL,
    canceled_utc DATETIME NULL,
    canceled_by_employee_number VARCHAR(32) NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_requests_queue_public_id (public_id),
    KEY idx_waitlist_requests_queue_building_status (building, status),
    KEY idx_waitlist_requests_queue_work_center (work_center),
    KEY idx_waitlist_requests_queue_requested_utc (requested_utc),
    KEY idx_waitlist_requests_queue_status (status)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
