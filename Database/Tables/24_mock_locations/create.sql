-- Create table: mock_locations
-- Engine: MySQL 5.7
-- Purpose: Mock inventory location catalog (Infor location codes) including the plant ignored
--          locations (WC, NCM, V-WC, NCM-VITS, SHIP) so filtering rules can be exercised.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_locations (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    location_code VARCHAR(50) NOT NULL COMMENT 'Infor location code (e.g. V-A0-01, WC, NCM).',
    location_description VARCHAR(255) NULL COMMENT 'Human-readable location description.',
    is_ignored TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the location is in the ignored set (WC/NCM/etc.).',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock location is active.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_locations_public_id (public_id),
    UNIQUE KEY uq_mock_locations_location_code (location_code)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
