-- Create table: core_buildings_history
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS core_buildings_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building_id BIGINT NOT NULL,
    building_code VARCHAR(64) NOT NULL,
    building_name VARCHAR(128) NOT NULL,
    is_active TINYINT(1) NOT NULL,
    change_action VARCHAR(16) NOT NULL,
    changed_by_user_id BIGINT NULL,
    changed_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_buildings_history_public_id (public_id),
    KEY idx_core_buildings_history_building_id_changed_utc (building_id, changed_utc),
    KEY idx_core_buildings_history_changed_by_user_id (changed_by_user_id),
    CONSTRAINT fk_core_buildings_history_core_buildings_catalog_building_id FOREIGN KEY (building_id) REFERENCES core_buildings_catalog (id),
    CONSTRAINT fk_core_buildings_history_core_users_profiles_changed_by_user_id FOREIGN KEY (changed_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;