-- Create table: config_workstation_hot_workcenters
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_workstation_hot_workcenters (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    core_workstation_id BIGINT NOT NULL,
    setup_workstation_id BIGINT NOT NULL,
    sort_rank INT NOT NULL DEFAULT 100,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_workstation_hot_workcenters_public_id (public_id),
    UNIQUE KEY uq_config_hot_workcenters_core_workstation_setup_workstation (
        core_workstation_id,
        setup_workstation_id
    ),
    KEY idx_config_hot_workcenters_core_workstation_active_sort (
        core_workstation_id,
        is_active,
        sort_rank
    ),
    CONSTRAINT fk_config_hot_workcenters_core_workstation_id FOREIGN KEY (core_workstation_id) REFERENCES core_workstations_registry (id),
    CONSTRAINT fk_config_hot_workcenters_setup_workstation_id FOREIGN KEY (setup_workstation_id) REFERENCES setup_workstations_catalog (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
