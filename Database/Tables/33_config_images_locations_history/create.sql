-- Create table: config_images_locations_history
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T005)
-- Purpose: record who set or replaced a picture, when, and what the previous picture was (FR-027).
--
-- One row per set or replace. The row is written in the same transaction as the picture row it describes, so a
-- change is never recorded without happening or happening without being recorded.
--
-- `scope` and `scope_item_id` are copied at write time rather than read through the foreign key, so the record
-- still says which system and which part it was about even if the picture row is later retired. That is why the
-- foreign key to the picture row is ON DELETE SET NULL instead of CASCADE: retiring a picture must not erase its
-- history.
--
-- `previous_image_path` is NULL for a first picture and otherwise holds the relative path the new picture
-- replaced. That column is the whole reason this table exists: the picture row's own actor and timestamp say who
-- touched it last and hold no previous value, so they cannot answer FR-027 for a second replacement.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_images_locations_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    image_location_id BIGINT NULL COMMENT 'The picture row this change belongs to; NULL once that row is retired',
    scope VARCHAR(16) NOT NULL COMMENT 'Copied at write time: request_item, request_category, work_center, visual_part, wip_part',
    scope_item_id VARCHAR(190) NOT NULL COMMENT 'Copied at write time: the item code, category code, work center id, or part number',
    previous_image_path VARCHAR(500) NULL COMMENT 'The relative path this change replaced; NULL for a first picture',
    new_image_path VARCHAR(500) NOT NULL COMMENT 'The relative path that replaced it',
    changed_by_user_id BIGINT NULL COMMENT 'The actor who set or replaced the picture',
    changed_utc DATETIME NOT NULL COMMENT 'UTC timestamp of the change',
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_images_locations_history_public_id (public_id),
    KEY idx_config_images_locations_history_scope_item (scope, scope_item_id, changed_utc) COMMENT 'Read one part''s record newest first',
    KEY idx_config_images_locations_history_image_location_id (image_location_id),
    KEY idx_config_images_locations_history_changed_by_user_id (changed_by_user_id),
    CONSTRAINT fk_config_images_locations_history_image_location_id FOREIGN KEY (image_location_id) REFERENCES config_images_locations (id) ON DELETE SET NULL,
    CONSTRAINT fk_config_images_locations_history_changed_by_user_id FOREIGN KEY (changed_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Who set or replaced a stored picture, when, and what the previous picture was.';

SET FOREIGN_KEY_CHECKS = 1;
