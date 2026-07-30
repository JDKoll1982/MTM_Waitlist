-- Create table: ops_request_types_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS ops_request_types_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    request_type_name VARCHAR(128) NOT NULL,
    request_type_name_normalized VARCHAR(128) NOT NULL,
    image_file_path VARCHAR(512) NULL,
    created_by_username VARCHAR(128) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ops_request_types_catalog_public_id (public_id),
    UNIQUE KEY uq_ops_request_types_catalog_request_type_name_normalized (request_type_name_normalized),
    KEY idx_ops_request_types_catalog_is_active (is_active)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
