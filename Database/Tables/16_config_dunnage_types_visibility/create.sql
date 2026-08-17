-- Create table: config_dunnage_types_visibility
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_dunnage_types_visibility (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    dunnage_type_id BIGINT NOT NULL,
    dunnage_type_name VARCHAR(128) NOT NULL,
    is_visible TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_dunnage_types_visibility_public_id (public_id),
    UNIQUE KEY uq_config_dunnage_types_visibility_dunnage_type_id (dunnage_type_id),
    KEY idx_config_dunnage_types_visibility_is_visible (is_visible),
    KEY idx_config_dunnage_types_visibility_updated_utc (updated_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
