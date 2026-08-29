-- Create table: core_computers_registry
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS core_computers_registry (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    computer_name VARCHAR(128) NOT NULL,
    hostname_normalized VARCHAR(255) NOT NULL,
    mac_address_normalized VARCHAR(64) NOT NULL,
    display_name VARCHAR(128) NOT NULL,
    description VARCHAR(255) NULL,
    is_registered TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_computers_registry_public_id (public_id),
    UNIQUE KEY uq_core_computers_registry_computer_mac_address (
        computer_name,
        mac_address_normalized
    ),
    UNIQUE KEY uq_core_computers_registry_display_name (display_name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;