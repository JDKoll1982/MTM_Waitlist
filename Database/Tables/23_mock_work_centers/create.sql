-- Create table: mock_work_centers
-- Engine: MySQL 5.7
-- Purpose: Mock work center / press catalog (Infor work-center codes) used by request mock data.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_work_centers (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    work_center_code VARCHAR(32) NOT NULL COMMENT 'Infor work center code (e.g. 100-3).',
    work_center_description VARCHAR(255) NULL COMMENT 'Human-readable work center description.',
    building VARCHAR(128) NULL COMMENT 'Building the work center belongs to.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock work center is active.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_work_centers_public_id (public_id),
    UNIQUE KEY uq_mock_work_centers_work_center_code (work_center_code)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
