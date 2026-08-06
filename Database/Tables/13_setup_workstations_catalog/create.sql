-- Create table: setup_workstations_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_workstations_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    workstation_name VARCHAR(64) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    sort_rank INT NOT NULL DEFAULT 100,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_workstations_catalog_public_id (public_id),
    UNIQUE KEY uq_setup_workstations_catalog_workstation_name (workstation_name),
    KEY idx_setup_workstations_catalog_is_active (is_active),
    KEY idx_setup_workstations_catalog_sort_rank (sort_rank)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;