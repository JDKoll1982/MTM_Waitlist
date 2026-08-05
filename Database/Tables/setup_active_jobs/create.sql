-- Create table: setup_active_jobs
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_active_jobs (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    work_order VARCHAR(32) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number VARCHAR(32) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    selected_dunnage_type_id VARCHAR(64) NULL,
    selected_dunnage_part_id VARCHAR(64) NULL,
    subordinate_parts_json JSON NULL,
    selected_dunnage_parts_json JSON NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_active_jobs_public_id (public_id),
    UNIQUE KEY uq_setup_active_jobs_work_center (work_center),
    KEY idx_setup_active_jobs_work_order (work_order),
    KEY idx_setup_active_jobs_updated_utc (updated_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;