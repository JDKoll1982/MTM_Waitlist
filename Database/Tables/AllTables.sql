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
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_roles_catalog_public_id (public_id),
    UNIQUE KEY uq_auth_roles_catalog_role_code (role_code)
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

-- Create table: auth_sessions_tokens
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS auth_sessions_tokens (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    computer_id BIGINT NULL,
    token_hash CHAR(64) NOT NULL,
    token_salt VARBINARY(32) NOT NULL,
    token_version SMALLINT NOT NULL DEFAULT 1,
    issued_utc DATETIME NOT NULL,
    expires_utc DATETIME NOT NULL,
    revoked_utc DATETIME NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    source_label VARCHAR(32) NOT NULL,
    created_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_auth_sessions_tokens_public_id (public_id),
    KEY idx_auth_sessions_tokens_user_id_expires_utc (user_id, expires_utc),
    KEY idx_auth_sessions_tokens_computer_id_expires_utc (computer_id, expires_utc),
    KEY idx_auth_sessions_tokens_is_active_expires_utc (is_active, expires_utc),
    CONSTRAINT fk_auth_sessions_tokens_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_auth_sessions_tokens_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- Create table: core_buildings_catalog
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS core_buildings_catalog (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building_code VARCHAR(64) NOT NULL,
    building_name VARCHAR(128) NOT NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    updated_by_user_id BIGINT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_buildings_catalog_public_id (public_id),
    UNIQUE KEY uq_core_buildings_catalog_building_code (building_code),
    UNIQUE KEY uq_core_buildings_catalog_building_name (building_name),
    KEY idx_core_buildings_catalog_is_active_building_name (is_active, building_name),
    CONSTRAINT fk_buildings_users_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Create table: core_buildings_history
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS core_buildings_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building_id BIGINT NOT NULL,
    building_code VARCHAR(64) NOT NULL,
    building_name VARCHAR(128) NOT NULL,
    is_active TINYINT(1) NOT NULL,
    change_action VARCHAR(16) NOT NULL,
    changed_by_user_id BIGINT NULL,
    changed_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_buildings_history_public_id (public_id),
    KEY idx_core_buildings_history_building_id_changed_utc (building_id, changed_utc),
    KEY idx_core_buildings_history_changed_by_user_id (changed_by_user_id),
    CONSTRAINT fk_buildings_history_building_id FOREIGN KEY (building_id) REFERENCES core_buildings_catalog (id),
    CONSTRAINT fk_buildings_history_changed_by_user_id FOREIGN KEY (changed_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

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
    message TEXT NOT NULL,
    payload_json MEDIUMTEXT NULL,
    previous_hash CHAR(64) NULL,
    entry_hash CHAR(64) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ops_startup_logs_public_id (public_id),
    KEY idx_ops_startup_logs_created_utc (created_utc),
    KEY idx_ops_startup_logs_correlation_id (correlation_id)
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

-- Create table: waitlist_requests_queue

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS waitlist_requests_queue (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    building VARCHAR(64) NOT NULL,
    work_center VARCHAR(64) NOT NULL,
    request_type VARCHAR(64) NOT NULL,
    subtype VARCHAR(64) NULL,
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
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_waitlist_requests_queue_public_id (public_id),
    KEY idx_waitlist_requests_queue_building_status (building, status),
    KEY idx_waitlist_requests_queue_work_center (work_center),
    KEY idx_waitlist_requests_queue_requested_utc (requested_utc),
    KEY idx_waitlist_requests_queue_status (status)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;