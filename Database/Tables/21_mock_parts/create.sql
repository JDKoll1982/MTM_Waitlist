-- Create table: mock_parts
-- Engine: MySQL 5.7
-- Purpose: Mock master part catalog used to build New-Request mock data. Part numbers follow the
--          plant scheme: MMC######## coils, MMF######## flatstock, and 23-###-#### for distinct
--          "Other" items. Each row carries the base fields the request-creation code expects.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_parts (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    part_number VARCHAR(50) NOT NULL COMMENT 'Mock part number (MMC######## / MMF######## / 23-###-####).',
    part_category VARCHAR(32) NOT NULL COMMENT 'Category: Coil, Flatstock, Other, Die, Component.',
    part_description VARCHAR(500) NULL COMMENT 'Human-readable part description.',
    unit_of_measure VARCHAR(20) NULL COMMENT 'Unit of measure (e.g. lb, each).',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock part is active for new requests.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_parts_public_id (public_id),
    UNIQUE KEY uq_mock_parts_part_number (part_number),
    KEY idx_mock_parts_part_category (part_category)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
