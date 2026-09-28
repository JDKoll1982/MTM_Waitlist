-- Create table: core_users_profiles
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS core_users_profiles (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    username_normalized VARCHAR(128) NOT NULL,
    first_name VARCHAR(128) NOT NULL,
    last_name VARCHAR(128) NOT NULL,
    password_hash VARCHAR(128) NOT NULL DEFAULT '0000',
    password_salt VARBINARY(32) NULL,
    require_password_change TINYINT(1) NOT NULL DEFAULT 1,
    temporary_credential_failed_attempts INT NOT NULL DEFAULT 0,
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

-- Create table: auth_roles_assignments
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_roles_assignments (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    role_id BIGINT NOT NULL,
    assigned_utc DATETIME NOT NULL,
    assigned_by_user_id BIGINT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_roles_assignments_public_id (public_id),
    UNIQUE KEY uq_auth_roles_assignments_user_role (user_id, role_id),
    KEY idx_auth_roles_assignments_role_id (role_id),
    KEY idx_auth_roles_assignments_assigned_by_user_id (assigned_by_user_id),
    CONSTRAINT fk_auth_roles_assignments_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_auth_roles_assignments_auth_roles_catalog_role_id FOREIGN KEY (role_id) REFERENCES auth_roles_catalog (id),
    CONSTRAINT fk_roles_assignments_users_assigned_by_user_id FOREIGN KEY (assigned_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: config_settings_values
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_settings_values (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    setting_key VARCHAR(190) NOT NULL,
    scope_type VARCHAR(16) NOT NULL DEFAULT 'all_users',
    scope_key VARCHAR(255) NOT NULL DEFAULT 'all_users',
    computer_id BIGINT NULL,
    user_id BIGINT NULL,
    setting_value TEXT NULL,
    setting_value_int BIGINT NULL,
    setting_value_bool TINYINT(1) NULL,
    setting_value_decimal DECIMAL(18, 6) NULL,
    setting_value_datetime_utc DATETIME NULL,
    value_type VARCHAR(32) NOT NULL,
    updated_by_user_id BIGINT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_settings_values_public_id (public_id),
    UNIQUE KEY uq_config_settings_values_setting_scope (setting_key, scope_key),
    KEY idx_config_settings_values_updated_by_user_id (updated_by_user_id),
    KEY idx_config_settings_values_computer_id (computer_id),
    KEY idx_config_settings_values_user_id (user_id),
    CONSTRAINT fk_values_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_config_settings_values_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id),
    CONSTRAINT fk_values_users_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: config_settings_history
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_settings_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    config_setting_id BIGINT NOT NULL,
    setting_key VARCHAR(190) NOT NULL,
    scope_type VARCHAR(16) NOT NULL,
    scope_key VARCHAR(255) NOT NULL,
    computer_id BIGINT NULL,
    user_id BIGINT NULL,
    previous_setting_value TEXT NULL,
    previous_setting_value_int BIGINT NULL,
    previous_setting_value_bool TINYINT(1) NULL,
    previous_setting_value_decimal DECIMAL(18, 6) NULL,
    previous_setting_value_datetime_utc DATETIME NULL,
    changed_setting_value TEXT NULL,
    changed_setting_value_int BIGINT NULL,
    changed_setting_value_bool TINYINT(1) NULL,
    changed_setting_value_decimal DECIMAL(18, 6) NULL,
    changed_setting_value_datetime_utc DATETIME NULL,
    value_type VARCHAR(32) NOT NULL,
    changed_by_user_id BIGINT NULL,
    changed_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_settings_history_public_id (public_id),
    KEY idx_config_settings_history_config_setting_id_changed_utc (
        config_setting_id,
        changed_utc
    ),
    KEY idx_config_settings_history_changed_by_user_id (changed_by_user_id),
    KEY idx_config_settings_history_setting_scope_changed_utc (
        setting_key,
        scope_key,
        changed_utc
    ),
    CONSTRAINT fk_settings_history_settings_values_config_setting_id FOREIGN KEY (config_setting_id) REFERENCES config_settings_values (id),
    CONSTRAINT fk_settings_history_users_changed_by_user_id FOREIGN KEY (changed_by_user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_config_settings_history_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id),
    CONSTRAINT fk_history_users_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: ops_startup_logs
-- Engine: MySQL 5.7
-- Feature: the log store is the existing one; 010-startup-rebuild (task T047) widened it, and this file is that
--          widened shape rather than a second table.
--
-- This is the one log store. The launch pipeline, the shell, Settings and the module libraries all write to it
-- through `sp_ops_startup_logs_insert`, which is its only writer, and read it through the filter and group
-- procedures. Nothing on the machine records a substitute entry when the store refuses a write (FR-025).
--
-- ============================================================
-- 010-startup-rebuild (task T047, plan D19): four columns and six indexes
-- ============================================================
-- Four columns were added for the developer panel, and each answers a question the previous shape could not:
--
--   * `module` names the part of the application an entry came from, so the panel can filter to one module
--     instead of reading every entry. It is also where an `ILogger` category name lands.
--   * `error_type` names the fault's type, which is what a reader searches by when they know what broke.
--   * `exception_detail` holds the serialized exception chain as a JSON array, one node per exception,
--     outermost first (contracts/logging-contract.md section 1.1). It is one column and not a child table on
--     purpose: the entry's hash covers one row, and splitting the chain would break the chain the store keeps.
--   * `error_fingerprint` is the SHA-256 of the fault's shape, so the same fault hashes the same on any machine
--     (FR-033, SC-014). It is what makes grouping possible, and it is NULL for an entry raised without an
--     exception.
--
-- All four are NULLable. A store that already holds entries has none of these values for them, and an entry
-- that is merely informational has no fault to describe.
--
-- Six indexes were added, one per filter the panel offers, each keyed on its own column followed by
-- `created_utc`. The order is the one every panel query uses: equality on the filter column, then a range over
-- the window, newest first. `created_utc` is second rather than first because the panel always filters before
-- it orders, and a leading `created_utc` would make each index a second copy of the existing one.
--
-- The chain columns are untouched and the table stays append-only: nothing updates or removes an entry except
-- `sp_ops_startup_logs_purge`, which removes only the oldest entries and only within the retention the operator
-- set.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS ops_startup_logs (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    correlation_id CHAR(36) NOT NULL,
    created_utc DATETIME NOT NULL,
    level VARCHAR(16) NOT NULL,
    event_action VARCHAR(128) NOT NULL,
    outcome VARCHAR(32) NOT NULL,
    actor_kind VARCHAR(32) NULL,
    actor_id VARCHAR(128) NULL,
    host_id VARCHAR(128) NULL,
    mac_address VARCHAR(64) NULL,
    module VARCHAR(64) NULL COMMENT 'The part of the application the entry came from; an ILogger category name lands here',
    error_type VARCHAR(128) NULL COMMENT 'The fault type, when there was a fault',
    exception_detail MEDIUMTEXT NULL COMMENT 'The serialized exception chain, one JSON node per exception, outermost first',
    error_fingerprint CHAR(64) NULL COMMENT 'SHA-256 of the fault shape, so the same fault groups together across machines; NULL without an exception',
    message TEXT NOT NULL,
    payload_json MEDIUMTEXT NULL,
    previous_hash CHAR(64) NULL,
    entry_hash CHAR(64) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ops_startup_logs_public_id (public_id),
    KEY idx_ops_startup_logs_created_utc (created_utc),
    KEY idx_ops_startup_logs_correlation_id (correlation_id),
    KEY idx_ops_startup_logs_level_created_utc (level, created_utc) COMMENT 'Panel filter: severity',
    KEY idx_ops_startup_logs_host_id_created_utc (host_id, created_utc) COMMENT 'Panel filter: machine',
    KEY idx_ops_startup_logs_module_created_utc (module, created_utc) COMMENT 'Panel filter: module',
    KEY idx_ops_startup_logs_actor_id_created_utc (actor_id, created_utc) COMMENT 'Panel filter: person',
    KEY idx_ops_startup_logs_error_type_created_utc (error_type, created_utc) COMMENT 'Panel filter: fault type',
    KEY idx_ops_startup_logs_error_fingerprint_created_utc (error_fingerprint, created_utc) COMMENT 'Panel grouping and filter: fault fingerprint'
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: setup_active_jobs
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_active_jobs (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    work_order VARCHAR(32) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number VARCHAR(32) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    selected_dunnage_type_id VARCHAR(64) NULL,
    selected_dunnage_part_id VARCHAR(64) NULL,
    subordinate_parts_json JSON NULL,
    selected_dunnage_parts_json JSON NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_active_jobs_public_id (public_id),
    UNIQUE KEY uq_setup_active_jobs_work_center (work_center),
    KEY idx_setup_active_jobs_work_order (work_order),
    KEY idx_setup_active_jobs_updated_utc (updated_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: setup_job_history
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_job_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    active_job_id BIGINT NULL,
    event_action VARCHAR(32) NOT NULL,
    work_order VARCHAR(32) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number VARCHAR(32) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    selected_dunnage_type_id VARCHAR(64) NULL,
    selected_dunnage_part_id VARCHAR(64) NULL,
    subordinate_parts_json JSON NULL,
    selected_dunnage_parts_json JSON NULL,
    changed_by_user_id BIGINT NULL,
    changed_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_job_history_public_id (public_id),
    KEY idx_setup_job_history_active_job_id (active_job_id),
    KEY idx_setup_job_history_work_order (work_order),
    KEY idx_setup_job_history_changed_utc (changed_utc),
    CONSTRAINT fk_setup_job_history_setup_active_jobs_active_job_id FOREIGN KEY (active_job_id) REFERENCES setup_active_jobs (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: setup_work_centers_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_work_centers_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building VARCHAR(64) NOT NULL DEFAULT 'Expo Drive',
    work_center_name VARCHAR(64) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    sort_rank INT NOT NULL DEFAULT 100,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_work_centers_catalog_public_id (public_id),
    UNIQUE KEY uq_setup_work_centers_catalog_building_work_center_name (building, work_center_name),
    KEY idx_setup_work_centers_catalog_is_active (is_active),
    KEY idx_setup_work_centers_catalog_sort_rank (sort_rank)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: config_computer_hot_work_centers
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_computer_hot_work_centers (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    computer_id BIGINT NOT NULL,
    work_center_id BIGINT NOT NULL,
    sort_rank INT NOT NULL DEFAULT 100,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_computer_hot_work_centers_public_id (public_id),
    UNIQUE KEY uq_config_hot_work_centers_computer_work_center (
        computer_id,
        work_center_id
    ),
    KEY idx_config_hot_work_centers_computer_active_sort (
        computer_id,
        is_active,
        sort_rank
    ),
    CONSTRAINT fk_config_hot_work_centers_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id),
    CONSTRAINT fk_config_hot_work_centers_work_center_id FOREIGN KEY (work_center_id) REFERENCES setup_work_centers_catalog (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: setup_part_sequence_custom_data
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_part_sequence_custom_data (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number VARCHAR(32) NOT NULL,
    selected_scrap_type VARCHAR(128) NULL,
    selected_dunnage_type_id VARCHAR(64) NULL,
    selected_dunnage_part_id VARCHAR(64) NULL,
    subordinate_parts_json JSON NULL,
    selected_dunnage_parts_json JSON NULL,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_part_sequence_custom_data_public_id (public_id),
    UNIQUE KEY uq_setup_part_sequence_custom_data_part_sequence (part_number, sequence_number),
    KEY idx_setup_part_sequence_custom_data_updated_utc (updated_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: config_dunnage_types_visibility
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS config_dunnage_types_visibility (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    dunnage_type_id BIGINT NOT NULL,
    dunnage_type_name VARCHAR(128) NOT NULL,
    is_visible TINYINT(1) NOT NULL DEFAULT 1,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_config_dunnage_types_visibility_public_id (public_id),
    UNIQUE KEY uq_config_dunnage_types_visibility_dunnage_type_id (dunnage_type_id),
    KEY idx_config_dunnage_types_visibility_is_visible (is_visible),
    KEY idx_config_dunnage_types_visibility_updated_utc (updated_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

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

-- Create table: waitlist_requests_queue
-- Engine: MySQL 5.7
-- Re-keyed by specs/004-unified-card-item-picker: a request carries the chosen Category and Item, and no
-- longer a request type or a subtype (FR-004). Everything else in this table is untouched, including the
-- lifecycle columns and the status vocabulary, because the request lifecycle does not change with the Item
-- (FR-032).
-- No back-fill and no migration path: the database is reinstalled from seed, and this change invents no path
-- for rows that do not exist.
-- idx_waitlist_requests_queue_item_status is added with the columns: sp_waitlist_request_item_observed_average_get
-- groups by item and filters on status, and the re-key is what makes that read possible.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_requests_queue (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building VARCHAR(64) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    category VARCHAR(16) NOT NULL COMMENT 'The Category the requester chose: Pickup, Deliver, Assist or Other.',
    item VARCHAR(64) NOT NULL COMMENT 'The Item the requester chose: one of the catalogued Item codes.',
    input_value VARCHAR(255) NULL,
    active_setup_job_id VARCHAR(64) NOT NULL,
    work_center_name VARCHAR(64) NOT NULL,
    requester_employee_number VARCHAR(32) NOT NULL,
    requester_employee_name VARCHAR(128) NOT NULL,
    status VARCHAR(32) NOT NULL DEFAULT 'Pending',
    requested_utc DATETIME NOT NULL,
    target_time_utc DATETIME NULL,
    is_overdue TINYINT(1) NOT NULL DEFAULT 0,
    assigned_material_handler VARCHAR(128) NULL,
    cancellation_reason VARCHAR(255) NULL,
    canceled_utc DATETIME NULL,
    canceled_by_employee_number VARCHAR(32) NULL,
    note VARCHAR(500) NULL,
    accepted_utc DATETIME NULL,
    completed_utc DATETIME NULL,
    released_utc DATETIME NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_requests_queue_public_id (public_id),
    KEY idx_waitlist_requests_queue_building_status (building, status),
    KEY idx_waitlist_requests_queue_work_center (work_center),
    KEY idx_waitlist_requests_queue_requested_utc (requested_utc),
    KEY idx_waitlist_requests_queue_status (status),
    KEY idx_waitlist_requests_queue_item_status (item, status)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: waitlist_requests_audit
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_requests_audit (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    request_public_id CHAR(36) NOT NULL,
    from_status VARCHAR(32) NULL,
    to_status VARCHAR(32) NULL,
    event_type VARCHAR(32) NOT NULL,
    actor_employee_number VARCHAR(32) NULL,
    actor_employee_name VARCHAR(128) NULL,
    details VARCHAR(255) NULL,
    occurred_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_requests_audit_public_id (public_id),
    KEY idx_waitlist_requests_audit_request_public_id (request_public_id),
    KEY idx_waitlist_requests_audit_occurred_utc (occurred_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: waitlist_defect_types
-- Engine: MySQL 5.7
-- Purpose: Managed list of non-conforming (NCM) defect types that a Pickup NCM request references.
--          Backed by a Module_Settings editor (Admin/Developer) so a worker can pick an NCM defect
--          from a searchable list rather than typing a free-form value. Mirrors the CSV pickup-ncm
--          "defections" note (new mysql table + stored procedures + settings panel to add/remove).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_defect_types (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the defect type row.',
    defect_name VARCHAR(100) NOT NULL COMMENT 'Display name of the NCM defect type (e.g. Scratch, Dent, Wrong Color).',
    description VARCHAR(500) NULL COMMENT 'Optional short description shown in the editor/picker.',
    sort_order INT NOT NULL DEFAULT 0 COMMENT 'Display order within the picker list.',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the defect type is offered to workers.',
    created_by_user_id BIGINT NULL COMMENT 'User who created the defect type.',
    updated_by_user_id BIGINT NULL COMMENT 'User who last updated the defect type.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the defect type row was created.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the defect type row was last updated.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_defect_types_public_id (public_id),
    UNIQUE KEY uq_waitlist_defect_types_defect_name (defect_name),
    KEY idx_waitlist_defect_types_is_active (is_active)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Purpose: One row per catalogued request Item: how far the flow goes, whether an answer is required, the
--          answer's type, the prompt, the length limits, the offered options, the fields the Item's page
--          shows, and the Item's allotted minutes. This is what lets each of those change as data instead of
--          as code (FR-013, FR-015). The Item's existence, Category, order, umbrella verb and Line 2 template
--          stay in the code catalog; this table carries behaviour only.
-- Source: specs/004-unified-card-item-picker/data-model.md section 3 (the column set, types, nulls and
--         defaults) and specs/004-unified-card-item-picker/contracts/item-configuration.md sections 2-3.
--         Authored at design time from the retiring type/subtype catalogs and the per-Item field
--         definitions; the application never reads a design document (FR-027).
-- Twin: uq_waitlist_request_item_configs_item is the single-row-per-Item guarantee FR-014 needs: an Item
--       either has exactly one configuration or none, and no configuration is a reportable state rather
--       than a silently merged one.
-- Audit: updated_by_user_id is the auditing column the repo's other update procedures carry, and this
--        table's one writer (sp_waitlist_request_item_allotted_minutes_update) records it. It is deliberately
--        NOT part of the read contract: sp_waitlist_request_item_configs_get returns the configuration the
--        application consumes, and who last changed a figure is not part of that.
-- Rows are seeded by Database/Seeds/seed_waitlist_request_item_configs (24 rows: one per catalogued Item).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_request_item_configs (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the configuration row.',
    item VARCHAR(64) NOT NULL COMMENT 'Catalog item code (e.g. pickup-coil, deliver-wrong-flatstock, other). One row per Item.',
    category VARCHAR(16) NOT NULL COMMENT 'The Category the Item belongs to: Pickup, Deliver, Assist or Other.',
    control_flow VARCHAR(64) NOT NULL DEFAULT 'direct-to-confirmation' COMMENT 'How far the flow goes before confirmation: direct-to-confirmation or collect-input-then-confirm.',
    requires_answer TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the Details step asks for an answer for this Item.',
    answer_value_type VARCHAR(16) NULL COMMENT 'enum or text where an answer is required; NULL otherwise.',
    prompt_text VARCHAR(500) NULL COMMENT 'The question shown where an answer is required.',
    min_length INT NOT NULL DEFAULT 0 COMMENT 'Minimum length of a text answer; ignored where none is required.',
    max_length INT NOT NULL DEFAULT 200 COMMENT 'Maximum length of a text answer; ignored where none is required.',
    options_json JSON NULL COMMENT 'Ordered options for an enumerated answer, where the options are fixed by configuration.',
    detail_fields_json JSON NULL COMMENT 'The ordered fields the Item page shows: label, value_type, source, order, is_required.',
    allotted_minutes INT NULL COMMENT 'The Item configured allotment in minutes. NULL means not configured, and the application then uses the labelled 15-minute default.',
    updated_by_user_id BIGINT NULL COMMENT 'User who last changed this configuration row. Audit only; not part of the read contract.',
    created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the configuration row was created.',
    updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the configuration row was last updated.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_request_item_configs_public_id (public_id),
    UNIQUE KEY uq_waitlist_request_item_configs_item (item),
    KEY idx_waitlist_request_item_configs_category (category),
    CONSTRAINT fk_waitlist_request_item_configs_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'One row per catalogued request Item: flow, answer requirement, prompt, limits, options, page fields and allotted minutes. Behaviour as data.';

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: auth_user_management_audit
-- Engine: MySQL 5.7
-- Purpose: One row per account field that actually changed, so who changed what on whose account is
--          answerable after the fact (FR-108, FR-109, FR-110). It records changes and never attempts: a
--          refused write leaves no row here, because nothing changed (FR-026).
-- Shape: one row per changed field. A save that touches four fields writes four rows sharing one
--        change_group_id, so one act reads as one act (FR-110).
--        A password reset writes one row with BOTH value columns null, because the temporary credential is
--        never stored in readable form and there is no before or after value to record (FR-111, FR-030).
-- Actor snapshots: actor_display_name, actor_employee_identifier and actor_role_code are copied in rather
--        than joined, so a later rename or role change cannot re-attribute what somebody did (FR-109).
-- Readers: nothing in the application reads this table. It is written by the user-management procedures and
--        read by support.
-- No foreign keys, deliberately: the actor columns are snapshots and target_user_id must outlive the account
--        it names, so a constraint back to core_users_profiles would both contradict the snapshot rule and
--        block the hard delete of a person the audit talks about.
-- Retention: this feature defines no retention window for this table.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_user_management_audit (
    id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    public_id CHAR(36) NOT NULL COMMENT 'Public UUID for external references.',
    change_group_id CHAR(36) NOT NULL COMMENT 'Shared by every row one save produced, so one act reads as one act.',
    target_user_id BIGINT NOT NULL COMMENT 'The person whose account changed.',
    field_name VARCHAR(64) NOT NULL COMMENT 'The one field this row is about.',
    previous_value TEXT NULL COMMENT 'The value before the change, or NULL where there is none to record.',
    changed_value TEXT NULL COMMENT 'The value after the change, or NULL where there is none to record.',
    actor_user_id BIGINT NULL COMMENT 'The acting person, joined for durability.',
    actor_display_name VARCHAR(256) NOT NULL COMMENT 'The actor display name as it was when they acted.',
    actor_employee_identifier VARCHAR(128) NOT NULL COMMENT 'The actor employee number as it was when they acted.',
    actor_role_code VARCHAR(64) NOT NULL COMMENT 'The actor role code as it was when they acted.',
    occurred_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the change was recorded.',
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_user_management_audit_public_id (public_id),
    KEY idx_auth_user_management_audit_change_group_id (change_group_id),
    KEY idx_audit_target_user_id_occurred_utc (target_user_id, occurred_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

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

-- Create table: user_active_sessions
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T044)
-- Purpose: the one active session for a person on a machine (FR-010, FR-011).
--
-- One row per person per machine, enforced by `uq_user_active_sessions_user_computer` rather than by the
-- procedures, so a second sign-in on the same machine replaces this row instead of adding a second one. The
-- row is reused across sign-outs: signing out sets `revoked_utc` and `is_active = 0` in place, and the next
-- sign-in repoints the same row at a freshly issued token. Nothing here is ever inserted twice for one person
-- and one machine, which is why the key can be the pair and not the pair plus a status.
--
-- The token is held as a salted hash and never in plaintext (ruleset, "Session and Security"): `token_hash`
-- is the SHA-256 hex digest the caller computed and `token_salt` is the salt it used. A reader of this table
-- can replay nothing and can reverse nothing; the store only ever compares digests.
--
-- Validity is decided by the store's own clock, never the workstation's: the reader compares
-- `fn_server_utc_now()` against `expires_utc`, and this table stores no "now" of its own. `fn_server_utc_now`
-- is deliberately retained for exactly this reason (contracts/sql-contracts.md section 5).
--
-- `source_label` records where the session came from in the caller's vocabulary (for example `sign_in`,
-- `remembered_sign_in`), which is what lets the support reader tell a remembered session from a typed one
-- without storing anything about the credential.
--
-- No row is ever hard-deleted by the application: a signed-out or expired row is the evidence that the session
-- existed, and the retention of that evidence is not this table's decision to make.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS user_active_sessions (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    computer_id BIGINT NOT NULL,
    token_hash CHAR(64) NOT NULL COMMENT 'SHA-256 hex digest of the issued token; never the token itself',
    token_salt VARBINARY(32) NOT NULL COMMENT 'Salt the digest was computed with; present whenever a digest is',
    issued_utc DATETIME NOT NULL COMMENT 'When the token was issued, from the store clock',
    expires_utc DATETIME NOT NULL COMMENT 'When the token stops being valid, from the store clock',
    revoked_utc DATETIME NULL COMMENT 'When the session was cleared; NULL while it has not been',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Soft state: 0 once cleared, so the row survives as evidence',
    source_label VARCHAR(32) NOT NULL COMMENT 'Caller vocabulary for where the session came from',
    created_utc DATETIME NOT NULL COMMENT 'When this row was first written; unchanged by a later sign-in',
    PRIMARY KEY (id),
    UNIQUE KEY uq_user_active_sessions_public_id (public_id),
    UNIQUE KEY uq_user_active_sessions_user_computer (user_id, computer_id) COMMENT 'One active row per person per machine',
    KEY idx_user_active_sessions_is_active_expires_utc (is_active, expires_utc) COMMENT 'Live-session reads filter on this pair',
    KEY idx_user_active_sessions_computer_id (computer_id),
    CONSTRAINT fk_user_active_sessions_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_user_active_sessions_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'The one active session for a person on a machine: a salted token digest, its lifetime and its machine key.';

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: auth_remembered_sign_ins
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046, plan D12)
-- Purpose: the encrypted payload that lets a machine offer "remember me" without keeping a credential in
--          plaintext anywhere (FR-014).
--
-- The payload is written and read only by the application, which encrypts it before it arrives and decrypts it
-- after it leaves. This table therefore stores three things and understands none of them: `payload_ciphertext`,
-- the `payload_iv` it was encrypted with, and `key_fingerprint`, which names which key was used.
--
-- `key_fingerprint` is the reason a rotated key degrades instead of failing (FR-014). A fingerprint that no
-- longer matches the key in hand means the payload cannot be decrypted, and the sign-in form is shown rather
-- than an error being raised. Without the fingerprint the application would have to try to decrypt and treat
-- the failure as a fault, which is exactly the behaviour the requirement forbids.
--
-- One row per person per machine, enforced by `uq_auth_remembered_sign_ins_user_computer`, so a re-remember
-- replaces the payload in place. Clearing sets `is_active = 0` and blanks the payload rather than deleting the
-- row: a cleared row keeps its provenance and loses the only thing that could be decrypted. A read never
-- returns an inactive row, so a cleared payload cannot be decrypted by mistake.
--
-- `payload_iv` is never absent. A ciphertext without its initialisation vector cannot be decrypted at all, so a
-- row like that could only ever be a bug; the column is NOT NULL so the store refuses to hold one, and the
-- validation script asserts the same shape (contracts/sql-contracts.md section 2).

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_remembered_sign_ins (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    computer_id BIGINT NOT NULL,
    payload_ciphertext VARBINARY(512) NOT NULL COMMENT 'The encrypted remembered payload; opaque to the store',
    payload_iv VARBINARY(16) NOT NULL COMMENT 'The initialisation vector the ciphertext was produced with',
    key_fingerprint CHAR(64) NOT NULL COMMENT 'Identifies the key that encrypted the payload, so a rotated key is detected rather than raised',
    is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Soft state: 0 once cleared, and a cleared row is never read',
    created_utc DATETIME NOT NULL COMMENT 'When this person first used this machine to remember a sign-in',
    updated_utc DATETIME NOT NULL COMMENT 'When the payload was last written or cleared',
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_remembered_sign_ins_public_id (public_id),
    UNIQUE KEY uq_auth_remembered_sign_ins_user_computer (user_id, computer_id) COMMENT 'One remembered sign-in per person per machine',
    CONSTRAINT fk_auth_remembered_sign_ins_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_auth_remembered_sign_ins_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'An encrypted remembered sign-in per person and machine, with the initialisation vector and the fingerprint of the key that produced it.';

SET FOREIGN_KEY_CHECKS = 1;
