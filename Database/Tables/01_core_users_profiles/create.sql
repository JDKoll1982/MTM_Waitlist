-- Create table: core_users_profiles
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS core_users_profiles (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    username_normalized VARCHAR(128) NOT NULL,
    password_hash VARCHAR(128) NOT NULL DEFAULT '0000',
    password_salt VARBINARY(32) NULL,
    require_password_change TINYINT(1) NOT NULL DEFAULT 1,
    display_name VARCHAR(256) NOT NULL,
    employee_identifier VARCHAR(128) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_users_profiles_public_id (public_id),
    UNIQUE KEY uq_core_users_profiles_username_normalized (username_normalized)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;