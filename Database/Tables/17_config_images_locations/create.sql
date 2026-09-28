-- Create table: config_images_locations
-- Purpose: Store image path overrides for request types, work centers, and request subtypes
-- Engine: MySQL 5.7
-- Audit Trail: created_by_user_id, updated_by_user_id, created_utc, updated_utc
-- Constraints: Composite unique on (scope, scope_item_id) to prevent duplicate overrides
--
-- Feature 008-part-pictures (task T004) widens the `scope` column's comment to name the five values it now holds
-- and changes nothing else. No column is added, dropped or retyped, and `uq_config_images_locations_scope_item`
-- is untouched: for a part scope that key is the system and the part number together, which is exactly one
-- picture per part per system.
--
-- Feature 010-startup-rebuild (task T051) adds the sixth scope, `computer`, and the machine column that goes
-- with it. This is where a machine's picture sources are stored — the shared picture folder, the keys folder and
-- the dunnage root — because those are machine configuration, not columns on `core_computers_registry`.
--
-- ============================================================
-- 010-startup-rebuild (task T051): the machine scope
-- ============================================================
-- The five picture scopes answer "which picture does this item, category, work centre or part have". The sixth
-- answers a different question: "where does this machine read its pictures from". Each machine holds three rows,
-- one per source kind, and the source kind is what makes them three rows rather than one.
--
-- At `computer` scope, `scope_item_id` is `<computer_id>:<source_kind>` — for example `4:shared_folder` — with
-- these three kinds:
--
--   shared_folder  the root every stored picture sits under
--   keys_folder    the folder holding the shared key material
--   dunnage_root   the root the dunnage pictures sit under
--
-- A composite item id is not decoration. `uq_config_images_locations_scope_item` is what stops one scope holding
-- two pictures for the same item, and a machine's three folders have to repeat a scope without repeating an item.
-- The obvious alternative — adding the machine column to that unique key — does not work in MySQL, and the
-- failure is silent rather than loud: a UNIQUE key treats NULLs as distinct, so a nullable machine column would
-- let every one of the five item-like scopes hold unlimited duplicate rows from the moment it was added, quietly
-- losing the guarantee the table exists to provide. Putting the machine into the item id instead keeps the key,
-- and with it the guarantee, exactly as it was.
--
-- `computer_id` carries the same machine as a foreign key, so the row is joinable and a machine's rows cannot
-- outlive the machine: the key deletes with it, which is why a decommissioned computer leaves no picture-source
-- rows behind.
--
-- Nothing writes a `computer` scope row any more. The three folder values used to be machine configuration, read
-- by that machine's own launch; they are held once for the whole plant instead, in the storage settings the
-- settings panel writes, so no computer captures a folder of its own (FR-040). The writer and both readers of the
-- `computer` scope were retired by task T198, and the rows an older store holds are withdrawn by
-- `Database/Seeds/seed_retire_computer_scope_picture_sources`. The scope value and the column stay: a store that
-- already holds rows keeps them, and the history keeps reading what was there.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_images_locations (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    scope VARCHAR(16) NOT NULL COMMENT 'Scope type: request_item, request_category, work_center, visual_part, wip_part, computer',
    scope_item_id VARCHAR(190) NOT NULL COMMENT 'Identifier within scope: Item code, Category code, numeric work center id, or <computer_id>:<source_kind> at computer scope',
    computer_id BIGINT NULL COMMENT 'At computer scope, the machine the row belongs to; NULL for the five picture scopes',
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
    KEY idx_config_images_locations_computer_id (computer_id) COMMENT 'Read the six rows of one machine without scanning the scope',
    KEY idx_config_images_locations_created_by_user_id (created_by_user_id),
    KEY idx_config_images_locations_updated_by_user_id (updated_by_user_id),
    CONSTRAINT fk_config_images_locations_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id) ON DELETE CASCADE,
    CONSTRAINT fk_config_images_locations_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL,
    CONSTRAINT fk_config_images_locations_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Image location overrides for request types, work centers, request subtypes, parts and machines. Supports cascade resolution with JSON defaults and fallback assets.';

SET FOREIGN_KEY_CHECKS = 1;