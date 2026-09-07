-- Create table: waitlist_requests_audit
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_requests_audit (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    request_public_id CHAR(36) NOT NULL,
    from_status VARCHAR(32) NULL,
    to_status VARCHAR(32) NULL,
    event_type VARCHAR(32) NOT NULL,
    actor_employee_number VARCHAR(32) NULL,
    actor_employee_name VARCHAR(128) NULL,
    details VARCHAR(255) NULL,
    occurred_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_requests_audit_public_id (public_id),
    KEY idx_waitlist_requests_audit_request_public_id (request_public_id),
    KEY idx_waitlist_requests_audit_occurred_utc (occurred_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
