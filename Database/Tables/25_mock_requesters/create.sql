-- Create table: mock_requesters
-- Engine: MySQL 5.7
-- Purpose: Mock requester (employee) catalog used to attribute mock requests to real identities.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_requesters (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    employee_number VARCHAR(32) NOT NULL COMMENT 'Requester employee number.',
    display_name VARCHAR(128) NOT NULL COMMENT 'Requester display name shown in the UI.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock requester is active.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_requesters_public_id (public_id),
    UNIQUE KEY uq_mock_requesters_employee_number (employee_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
