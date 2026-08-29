-- Create table: config_computer_hot_work_centers
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_computer_hot_work_centers (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    computer_id BIGINT NOT NULL,
    work_center_id BIGINT NOT NULL,
    sort_rank INT NOT NULL DEFAULT 100,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_computer_hot_work_centers_public_id (public_id),
    UNIQUE KEY uq_config_hot_work_centers_computer_work_center (
        computer_id,
        work_center_id
    ),
    KEY idx_config_hot_work_centers_computer_active_sort (
        computer_id,
        is_active,
        sort_rank
    ),
    CONSTRAINT fk_config_hot_work_centers_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id),
    CONSTRAINT fk_config_hot_work_centers_work_center_id FOREIGN KEY (work_center_id) REFERENCES setup_work_centers_catalog (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
