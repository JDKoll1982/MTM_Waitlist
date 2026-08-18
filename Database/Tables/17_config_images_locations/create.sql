-- Create table: config_images_locations
-- Purpose: Store image path overrides for request types, work centers, and request subtypes
-- Engine: MySQL 5.7
-- Audit Trail: created_by_user_id, updated_by_user_id, created_utc, updated_utc
-- Constraints: Composite unique on (scope, scope_item_id) to prevent duplicate overrides

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_images_locations (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    scope VARCHAR(16) NOT NULL COMMENT 'Scope type: request_type, work_center, request_subtype',
    scope_item_id VARCHAR(190) NOT NULL COMMENT 'Stable ID within scope: GUID for types/subtypes, BIGINT for work centers',
    image_path VARCHAR(500) NOT NULL COMMENT 'File system path to the copied image',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Soft-delete flag; inactive rows are ignored during resolution',
    created_by_user_id BIGINT NULL COMMENT 'User who created this override',
    updated_by_user_id BIGINT NULL COMMENT 'User who last modified this override',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when override was created',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when override was last updated',
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_images_locations_public_id (public_id),
    UNIQUE KEY uq_config_images_locations_scope_item (scope, scope_item_id) COMMENT 'Ensure only one active override per scope/item pair',
    KEY idx_config_images_locations_scope_active (scope, is_active) COMMENT 'Composite index for scope queries with active filter',
    KEY idx_config_images_locations_created_by_user_id (created_by_user_id),
    KEY idx_config_images_locations_updated_by_user_id (updated_by_user_id),
    CONSTRAINT fk_config_images_locations_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL,
    CONSTRAINT fk_config_images_locations_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Image location overrides for request types, work centers, and request subtypes. Supports cascade resolution with JSON defaults and fallback assets.';

SET FOREIGN_KEY_CHECKS = 1;