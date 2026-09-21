-- Create table: auth_roles_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_roles_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    role_code VARCHAR(64) NOT NULL,
    role_name VARCHAR(128) NOT NULL,
    role_rank INT NOT NULL DEFAULT 0,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_roles_catalog_public_id (public_id),
    UNIQUE KEY uq_auth_roles_catalog_role_code (role_code),
    KEY idx_auth_roles_catalog_role_rank (role_rank)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;