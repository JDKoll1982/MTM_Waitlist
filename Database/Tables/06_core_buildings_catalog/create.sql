-- Create table: core_buildings_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS core_buildings_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building_code VARCHAR(64) NOT NULL,
    building_name VARCHAR(128) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    updated_by_user_id BIGINT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_buildings_catalog_public_id (public_id),
    UNIQUE KEY uq_core_buildings_catalog_building_code (building_code),
    UNIQUE KEY uq_core_buildings_catalog_building_name (building_name),
    KEY idx_core_buildings_catalog_is_active_building_name (is_active, building_name),
    CONSTRAINT fk_buildings_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;