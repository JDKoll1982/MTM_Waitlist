-- Create table: mock_inventory_locations
-- Engine: MySQL 5.7
-- Purpose: Mock per-part, per-location pooled inventory rows (Part # / Location / Quantity lb).
--          Mirrors Infor Visual: one pooled row per unique part-in-location with on-hand weight.
--          Includes ignored-location and zero-quantity rows to exercise the >= 1 and ignore rules.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_inventory_locations (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    part_number VARCHAR(50) NOT NULL COMMENT 'Part number (references mock_parts.part_number).',
    location_code VARCHAR(50) NOT NULL COMMENT 'Infor location code (references mock_locations.location_code).',
    on_hand_quantity DECIMAL(18,2) NOT NULL DEFAULT 0 COMMENT 'Pooled on-hand weight (lb) for the part at this location.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_inventory_locations_public_id (public_id),
    UNIQUE KEY uq_mock_inventory_locations_part_location (part_number, location_code),
    KEY idx_mock_inventory_locations_location_code (location_code)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
