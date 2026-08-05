-- Create table: setup_job_history
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_job_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    active_job_id BIGINT NULL,
    event_action VARCHAR(32) NOT NULL,
    work_order VARCHAR(32) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number VARCHAR(32) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    selected_dunnage_type_id VARCHAR(64) NULL,
    selected_dunnage_part_id VARCHAR(64) NULL,
    subordinate_parts_json JSON NULL,
    selected_dunnage_parts_json JSON NULL,
    changed_by_user_id BIGINT NULL,
    changed_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_job_history_public_id (public_id),
    KEY idx_setup_job_history_active_job_id (active_job_id),
    KEY idx_setup_job_history_work_order (work_order),
    KEY idx_setup_job_history_changed_utc (changed_utc),
    CONSTRAINT fk_setup_job_history_setup_active_jobs_active_job_id FOREIGN KEY (active_job_id) REFERENCES setup_active_jobs (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;