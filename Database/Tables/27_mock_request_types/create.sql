-- Create table: mock_request_types
-- Engine: MySQL 5.7
-- Purpose: Mock request-type/subtype catalog that drives which request types (and their actions,
--          e.g. Bring/Pickup) are offered when building a New-Request in mock mode.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS mock_request_types (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    request_type VARCHAR(64) NOT NULL COMMENT 'Request type (e.g. Coil, Pickup, Other, Scrap).',
    subtype VARCHAR(128) NULL COMMENT 'Request subtype/action (e.g. Bring, Pickup, Pickup Coil).',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the mock request type is offered.',
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_mock_request_types_public_id (public_id),
    KEY idx_mock_request_types_request_type (request_type)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
