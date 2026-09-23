-- Update script: apply table descriptions for mtm_waitlist
-- Engine: MySQL 5.7

USE mtm_waitlist;

ALTER TABLE core_users_profiles COMMENT = 'User profile and authentication identity records.';

-- Feature 006: the person's own name parts and the wrong-attempt count on a temporary credential.
-- A guarded block is required rather than optional here, because `CREATE TABLE IF NOT EXISTS` cannot add a
-- column to a table that already exists. The three blocks below follow the shape of the
-- `setup_work_centers_catalog.building` block further down this file.
SET
    @has_first_name_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_users_profiles'
            AND column_name = 'first_name'
    );

SET
    @sql_stmt := IF(
        @has_first_name_column = 0,
        'ALTER TABLE core_users_profiles ADD COLUMN first_name VARCHAR(128) NOT NULL AFTER username_normalized',
        'SELECT ''core_users_profiles.first_name already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_last_name_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_users_profiles'
            AND column_name = 'last_name'
    );

SET
    @sql_stmt := IF(
        @has_last_name_column = 0,
        'ALTER TABLE core_users_profiles ADD COLUMN last_name VARCHAR(128) NOT NULL AFTER first_name',
        'SELECT ''core_users_profiles.last_name already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_temporary_credential_failed_attempts_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'core_users_profiles'
            AND column_name = 'temporary_credential_failed_attempts'
    );

SET
    @sql_stmt := IF(
        @has_temporary_credential_failed_attempts_column = 0,
        'ALTER TABLE core_users_profiles ADD COLUMN temporary_credential_failed_attempts INT NOT NULL DEFAULT 0 AFTER require_password_change',
        'SELECT ''core_users_profiles.temporary_credential_failed_attempts already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

ALTER TABLE core_users_profiles
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for external references.',
MODIFY COLUMN username_normalized VARCHAR(128) NOT NULL COMMENT 'Normalized unique username for sign-in lookup.',
MODIFY COLUMN first_name VARCHAR(128) NOT NULL COMMENT 'The person given name, 1 to 128 characters.',
MODIFY COLUMN last_name VARCHAR(128) NOT NULL COMMENT 'The person family name, 1 to 128 characters.',
MODIFY COLUMN password_hash VARCHAR(128) NOT NULL DEFAULT '0000' COMMENT 'Password hash value for authentication.',
MODIFY COLUMN password_salt VARBINARY(32) NULL COMMENT 'Per-user password salt.',
MODIFY COLUMN require_password_change TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Flag requiring password reset on next login.',
MODIFY COLUMN temporary_credential_failed_attempts INT NOT NULL DEFAULT 0 COMMENT 'Wrong attempts made with a temporary credential. Cleared by a successful attempt or a fresh reset; never expires with time.',
MODIFY COLUMN display_name VARCHAR(256) NOT NULL COMMENT 'Display name shown in the UI.',
MODIFY COLUMN employee_identifier VARCHAR(128) NULL COMMENT 'Optional employee number or identifier.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the user account is active.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';

ALTER TABLE core_computers_registry COMMENT = 'Registered computer and host identity catalog.';

ALTER TABLE core_computers_registry
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for computer record.',
MODIFY COLUMN computer_name VARCHAR(128) NOT NULL COMMENT 'Friendly computer name.',
MODIFY COLUMN hostname_normalized VARCHAR(255) NOT NULL COMMENT 'Normalized host name used for identity matching.',
MODIFY COLUMN mac_address_normalized VARCHAR(64) NOT NULL COMMENT 'Normalized MAC address for computer identity.',
MODIFY COLUMN display_name VARCHAR(128) NOT NULL COMMENT 'User-facing display name for the computer.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Optional free-text description for the computer.',
MODIFY COLUMN is_registered TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the computer is currently registered.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';

ALTER TABLE auth_roles_catalog COMMENT = 'Role definitions used for RBAC authorization.';

-- Feature 006: the one rung per role, so the ladder is a readable row rather than a value copied into code.
SET
    @has_role_rank_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'auth_roles_catalog'
            AND column_name = 'role_rank'
    );

SET
    @sql_stmt := IF(
        @has_role_rank_column = 0,
        'ALTER TABLE auth_roles_catalog ADD COLUMN role_rank INT NOT NULL DEFAULT 0 AFTER role_name',
        'SELECT ''auth_roles_catalog.role_rank already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_role_rank_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'auth_roles_catalog'
            AND index_name = 'idx_auth_roles_catalog_role_rank'
    );

SET
    @sql_stmt := IF(
        @has_role_rank_index = 0,
        'ALTER TABLE auth_roles_catalog ADD KEY idx_auth_roles_catalog_role_rank (role_rank)',
        'SELECT ''idx_auth_roles_catalog_role_rank already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

ALTER TABLE auth_roles_catalog
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for role record.',
MODIFY COLUMN role_code VARCHAR(64) NOT NULL COMMENT 'Machine-friendly unique role code.',
MODIFY COLUMN role_name VARCHAR(128) NOT NULL COMMENT 'Human-readable role name.',
MODIFY COLUMN role_rank INT NOT NULL DEFAULT 0 COMMENT 'The role rung used by every rank comparison. Higher outranks lower. A retired role is deliberately left at the default 0.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';

ALTER TABLE auth_roles_assignments COMMENT = 'User-to-role assignment records for RBAC.';

ALTER TABLE auth_roles_assignments
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for assignment record.',
MODIFY COLUMN user_id BIGINT NOT NULL COMMENT 'Foreign key to core_users_profiles.id.',
MODIFY COLUMN role_id BIGINT NOT NULL COMMENT 'Foreign key to auth_roles_catalog.id.',
MODIFY COLUMN assigned_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the role was assigned.',
MODIFY COLUMN assigned_by_user_id BIGINT NULL COMMENT 'User who assigned the role.';

ALTER TABLE auth_sessions_tokens COMMENT = 'Session token hash metadata and lifecycle state.';

ALTER TABLE auth_sessions_tokens
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for session record.',
MODIFY COLUMN user_id BIGINT NOT NULL COMMENT 'Foreign key to core_users_profiles.id.',
MODIFY COLUMN computer_id BIGINT NULL COMMENT 'Optional foreign key to core_computers_registry.id.',
MODIFY COLUMN token_hash CHAR(64) NOT NULL COMMENT 'Hashed session token value.',
MODIFY COLUMN token_salt VARBINARY(32) NOT NULL COMMENT 'Salt used for token hashing.',
MODIFY COLUMN token_version SMALLINT NOT NULL DEFAULT 1 COMMENT 'Token schema/hash version.',
MODIFY COLUMN issued_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the token was issued.',
MODIFY COLUMN expires_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the token expires.',
MODIFY COLUMN revoked_utc DATETIME NULL COMMENT 'UTC timestamp when the token was revoked.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the token is currently active.',
MODIFY COLUMN source_label VARCHAR(32) NOT NULL COMMENT 'Token source label such as startup or login.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.';

ALTER TABLE core_buildings_catalog COMMENT = 'Active building and facility catalog.';

ALTER TABLE core_buildings_catalog
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for building record.',
MODIFY COLUMN building_code VARCHAR(64) NOT NULL COMMENT 'Unique building code.',
MODIFY COLUMN building_name VARCHAR(128) NOT NULL COMMENT 'Display name for building/facility.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the building is active.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the building record.';

ALTER TABLE core_buildings_history COMMENT = 'Audit history for building catalog changes.';

ALTER TABLE core_buildings_history
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for history record.',
MODIFY COLUMN building_id BIGINT NOT NULL COMMENT 'Foreign key to core_buildings_catalog.id.',
MODIFY COLUMN building_code VARCHAR(64) NOT NULL COMMENT 'Building code at time of change.',
MODIFY COLUMN building_name VARCHAR(128) NOT NULL COMMENT 'Building name at time of change.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL COMMENT 'Active state snapshot at time of change.',
MODIFY COLUMN change_action VARCHAR(16) NOT NULL COMMENT 'Change action such as insert, update, or deactivate.',
MODIFY COLUMN changed_by_user_id BIGINT NULL COMMENT 'User who performed the change.',
MODIFY COLUMN changed_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the change occurred.';

ALTER TABLE config_settings_values COMMENT = 'Current effective configuration setting values by scope.';

ALTER TABLE config_settings_values
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for setting row.',
MODIFY COLUMN setting_key VARCHAR(190) NOT NULL COMMENT 'Unique setting identifier key.',
MODIFY COLUMN scope_type VARCHAR(16) NOT NULL DEFAULT 'all_users' COMMENT 'Scope kind such as computer, user, or all_users.',
MODIFY COLUMN scope_key VARCHAR(255) NOT NULL DEFAULT 'all_users' COMMENT 'Scope discriminator key value.',
MODIFY COLUMN computer_id BIGINT NULL COMMENT 'Optional computer scope foreign key.',
MODIFY COLUMN user_id BIGINT NULL COMMENT 'Optional user scope foreign key.',
MODIFY COLUMN setting_value TEXT NULL COMMENT 'Text setting value payload.',
MODIFY COLUMN setting_value_int BIGINT NULL COMMENT 'Integer setting value payload.',
MODIFY COLUMN setting_value_bool TINYINT(1) NULL COMMENT 'Boolean setting value payload.',
MODIFY COLUMN setting_value_decimal DECIMAL(18, 6) NULL COMMENT 'Decimal setting value payload.',
MODIFY COLUMN setting_value_datetime_utc DATETIME NULL COMMENT 'Datetime setting value payload in UTC.',
MODIFY COLUMN value_type VARCHAR(32) NOT NULL COMMENT 'Declared value type for the setting payload.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the setting.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when setting was last updated.';

ALTER TABLE config_settings_history COMMENT = 'Audit trail for configuration setting changes.';

ALTER TABLE config_settings_history
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for history row.',
MODIFY COLUMN config_setting_id BIGINT NOT NULL COMMENT 'Foreign key to config_settings_values.id.',
MODIFY COLUMN setting_key VARCHAR(190) NOT NULL COMMENT 'Setting key snapshot at time of change.',
MODIFY COLUMN scope_type VARCHAR(16) NOT NULL COMMENT 'Scope type snapshot at time of change.',
MODIFY COLUMN scope_key VARCHAR(255) NOT NULL COMMENT 'Scope key snapshot at time of change.',
MODIFY COLUMN computer_id BIGINT NULL COMMENT 'Optional computer scope foreign key snapshot.',
MODIFY COLUMN user_id BIGINT NULL COMMENT 'Optional user scope foreign key snapshot.',
MODIFY COLUMN previous_setting_value TEXT NULL COMMENT 'Previous text setting value.',
MODIFY COLUMN previous_setting_value_int BIGINT NULL COMMENT 'Previous integer setting value.',
MODIFY COLUMN previous_setting_value_bool TINYINT(1) NULL COMMENT 'Previous boolean setting value.',
MODIFY COLUMN previous_setting_value_decimal DECIMAL(18, 6) NULL COMMENT 'Previous decimal setting value.',
MODIFY COLUMN previous_setting_value_datetime_utc DATETIME NULL COMMENT 'Previous datetime setting value in UTC.',
MODIFY COLUMN changed_setting_value TEXT NULL COMMENT 'New text setting value.',
MODIFY COLUMN changed_setting_value_int BIGINT NULL COMMENT 'New integer setting value.',
MODIFY COLUMN changed_setting_value_bool TINYINT(1) NULL COMMENT 'New boolean setting value.',
MODIFY COLUMN changed_setting_value_decimal DECIMAL(18, 6) NULL COMMENT 'New decimal setting value.',
MODIFY COLUMN changed_setting_value_datetime_utc DATETIME NULL COMMENT 'New datetime setting value in UTC.',
MODIFY COLUMN value_type VARCHAR(32) NOT NULL COMMENT 'Declared value type for history payload.',
MODIFY COLUMN changed_by_user_id BIGINT NULL COMMENT 'User who made the setting change.',
MODIFY COLUMN changed_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the change was recorded.';

ALTER TABLE ops_startup_logs COMMENT = 'Startup and operational telemetry log entries.';

ALTER TABLE ops_startup_logs
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for log record.',
MODIFY COLUMN correlation_id CHAR(36) NOT NULL COMMENT 'Correlation UUID for grouped startup events.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp of log event creation.',
MODIFY COLUMN level VARCHAR(16) NOT NULL COMMENT 'Log severity level.',
MODIFY COLUMN event_action VARCHAR(128) NOT NULL COMMENT 'Event action or operation name.',
MODIFY COLUMN outcome VARCHAR(32) NOT NULL COMMENT 'Outcome status for event.',
MODIFY COLUMN actor_kind VARCHAR(32) NULL COMMENT 'Actor type for event source.',
MODIFY COLUMN actor_id VARCHAR(128) NULL COMMENT 'Actor identifier value.',
MODIFY COLUMN host_id VARCHAR(128) NULL COMMENT 'Host/computer identifier captured for event.',
MODIFY COLUMN mac_address VARCHAR(64) NULL COMMENT 'MAC address captured for event.',
MODIFY COLUMN message TEXT NOT NULL COMMENT 'Primary log message text.',
MODIFY COLUMN payload_json MEDIUMTEXT NULL COMMENT 'Optional structured payload as JSON text.',
MODIFY COLUMN previous_hash CHAR(64) NULL COMMENT 'Previous entry hash for chain validation.',
MODIFY COLUMN entry_hash CHAR(64) NOT NULL COMMENT 'Hash for current log entry integrity.';

ALTER TABLE setup_active_jobs COMMENT = 'Current active setup job assignments by work center.';

ALTER TABLE setup_active_jobs
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for active job row.',
MODIFY COLUMN work_order VARCHAR(32) NOT NULL COMMENT 'Work order number.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number for the setup job.',
MODIFY COLUMN sequence_number VARCHAR(32) NOT NULL COMMENT 'Sequence number for the setup job.',
MODIFY COLUMN work_center VARCHAR(64) NOT NULL COMMENT 'Work center or press identifier.',
MODIFY COLUMN selected_dunnage_type_id VARCHAR(64) NULL COMMENT 'Selected dunnage type identifier.',
MODIFY COLUMN selected_dunnage_part_id VARCHAR(64) NULL COMMENT 'Selected dunnage part identifier.',
MODIFY COLUMN subordinate_parts_json JSON NULL COMMENT 'Serialized subordinate parts payload for the active work-center setup row.',
MODIFY COLUMN selected_dunnage_parts_json JSON NULL COMMENT 'Serialized selected dunnage parts payload.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the active job row is active.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the active job row.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the active job row.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE setup_job_history COMMENT = 'Historical setup job events and saved setup snapshots.';

ALTER TABLE setup_job_history
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for history row.',
MODIFY COLUMN active_job_id BIGINT NULL COMMENT 'Optional foreign key to setup_active_jobs.id.',
MODIFY COLUMN event_action VARCHAR(32) NOT NULL COMMENT 'Event action for history row.',
MODIFY COLUMN work_order VARCHAR(32) NOT NULL COMMENT 'Work order at time of event.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number at time of event.',
MODIFY COLUMN sequence_number VARCHAR(32) NOT NULL COMMENT 'Sequence number at time of event.',
MODIFY COLUMN work_center VARCHAR(64) NOT NULL COMMENT 'Work center at time of event.',
MODIFY COLUMN selected_dunnage_type_id VARCHAR(64) NULL COMMENT 'Selected dunnage type identifier snapshot.',
MODIFY COLUMN selected_dunnage_part_id VARCHAR(64) NULL COMMENT 'Selected dunnage part identifier snapshot.',
MODIFY COLUMN subordinate_parts_json JSON NULL COMMENT 'Serialized subordinate parts snapshot, including saved scrap selection for the exact work order/part/sequence event.',
MODIFY COLUMN selected_dunnage_parts_json JSON NULL COMMENT 'Serialized selected dunnage parts snapshot.',
MODIFY COLUMN changed_by_user_id BIGINT NULL COMMENT 'User who caused the history event.',
MODIFY COLUMN changed_utc DATETIME NOT NULL COMMENT 'UTC timestamp when history row was recorded.';

ALTER TABLE setup_work_centers_catalog COMMENT = 'Setup work center catalog used by setup and waitlist flows.';

SET
    @has_building_column := (
        SELECT COUNT(*)
        FROM information_schema.columns
        WHERE
            table_schema = DATABASE()
            AND table_name = 'setup_work_centers_catalog'
            AND column_name = 'building'
    );

SET
    @sql_stmt := IF(
        @has_building_column = 0,
        'ALTER TABLE setup_work_centers_catalog ADD COLUMN building VARCHAR(64) NOT NULL DEFAULT ''Expo Drive'' AFTER public_id',
        'SELECT ''setup_work_centers_catalog.building already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_legacy_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'setup_work_centers_catalog'
            AND index_name = 'uq_setup_work_centers_catalog_work_center_name'
    );

SET
    @sql_stmt := IF(
        @has_legacy_index > 0,
        'ALTER TABLE setup_work_centers_catalog DROP INDEX uq_setup_work_centers_catalog_work_center_name',
        'SELECT ''uq_setup_work_centers_catalog_work_center_name not present'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET
    @has_building_index := (
        SELECT COUNT(*)
        FROM information_schema.statistics
        WHERE
            table_schema = DATABASE()
            AND table_name = 'setup_work_centers_catalog'
            AND index_name = 'uq_setup_work_centers_catalog_building_work_center_name'
    );

SET
    @sql_stmt := IF(
        @has_building_index = 0,
        'ALTER TABLE setup_work_centers_catalog ADD UNIQUE KEY uq_setup_work_centers_catalog_building_work_center_name (building, work_center_name)',
        'SELECT ''uq_setup_work_centers_catalog_building_work_center_name already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

ALTER TABLE setup_work_centers_catalog
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for work center catalog row.',
MODIFY COLUMN building VARCHAR(64) NOT NULL DEFAULT 'Expo Drive' COMMENT 'Facility building where the work center is located.',
MODIFY COLUMN work_center_name VARCHAR(64) NOT NULL COMMENT 'Work center or press display name.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether this work center is active.',
MODIFY COLUMN sort_rank INT NOT NULL DEFAULT 100 COMMENT 'Sort order rank for UI lists.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the work center row.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the work center row.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE config_computer_hot_work_centers COMMENT = 'Per-computer Local work center preferences and ordering.';

ALTER TABLE config_computer_hot_work_centers
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for Local mapping row.',
MODIFY COLUMN computer_id BIGINT NOT NULL COMMENT 'Foreign key to core_computers_registry.id.',
MODIFY COLUMN work_center_id BIGINT NOT NULL COMMENT 'Foreign key to setup_work_centers_catalog.id.',
MODIFY COLUMN sort_rank INT NOT NULL DEFAULT 100 COMMENT 'Display order for Local work centers per computer.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether this Local mapping is active.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the mapping row.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the mapping row.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE setup_part_sequence_custom_data COMMENT = 'Custom setup data keyed by part and sequence for values not sourced from Infor Visual.';

ALTER TABLE setup_part_sequence_custom_data
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for custom pair row.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number key for custom setup data.',
MODIFY COLUMN sequence_number VARCHAR(32) NOT NULL COMMENT 'Sequence number key for custom setup data.',
MODIFY COLUMN selected_scrap_type VARCHAR(128) NULL COMMENT 'Saved scrap selection for the part/sequence pair.',
MODIFY COLUMN selected_dunnage_type_id VARCHAR(64) NULL COMMENT 'Saved dunnage type identifier for the part/sequence pair.',
MODIFY COLUMN selected_dunnage_part_id VARCHAR(64) NULL COMMENT 'Saved dunnage part identifier for the part/sequence pair.',
MODIFY COLUMN subordinate_parts_json JSON NULL COMMENT 'Serialized subordinate parts payload for the part/sequence pair.',
MODIFY COLUMN selected_dunnage_parts_json JSON NULL COMMENT 'Serialized selected dunnage parts payload for the part/sequence pair.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the custom pair row.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the custom pair row.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE config_dunnage_types_visibility COMMENT = 'Global dunnage type visibility preferences for MTM Waitlist.';

ALTER TABLE config_dunnage_types_visibility
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for visibility row.',
MODIFY COLUMN dunnage_type_id BIGINT NOT NULL COMMENT 'Source dunnage type identifier from mtm_receiving_application.dunnage_types.id.',
MODIFY COLUMN dunnage_type_name VARCHAR(128) NOT NULL COMMENT 'Dunnage type display name snapshot.',
MODIFY COLUMN is_visible TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the dunnage type is visible in MTM Waitlist.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the visibility row.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the visibility row.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE config_images_locations COMMENT = 'Image path overrides for request items, their Category families, and work centers. Enables role-based customization of visual assets via cascade resolution pattern.';

ALTER TABLE config_images_locations
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for image location override row.',
MODIFY COLUMN scope VARCHAR(16) NOT NULL COMMENT 'Scope type: request_item, request_category, work_center, visual_part, or wip_part.',
MODIFY COLUMN scope_item_id VARCHAR(190) NOT NULL COMMENT 'Identifier within scope: the Item or Category code, or the numeric work center id.',
MODIFY COLUMN image_path VARCHAR(500) NOT NULL COMMENT 'File system path to the image file copied to the shared network folder.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Soft-delete flag; inactive rows are ignored during path resolution cascades.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the image override.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the image override.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when override was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when override was last updated.';

ALTER TABLE waitlist_requests_queue COMMENT = 'Submitted material-handling requests awaiting pickup, in progress, or resolved.';

ALTER TABLE waitlist_requests_queue
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the waitlist request.',
MODIFY COLUMN building VARCHAR(64) NOT NULL COMMENT 'Building the request belongs to.',
MODIFY COLUMN work_center VARCHAR(64) NOT NULL COMMENT 'Work center that raised the request.',
MODIFY COLUMN category VARCHAR(16) NOT NULL COMMENT 'The Category the requester chose: Pickup, Deliver, Assist or Other.',
MODIFY COLUMN item VARCHAR(64) NOT NULL COMMENT 'The Item the requester chose: one of the catalogued Item codes.',
MODIFY COLUMN input_value VARCHAR(255) NULL COMMENT 'The answer captured for an Item whose flow collects one.',
MODIFY COLUMN active_setup_job_id VARCHAR(64) NOT NULL COMMENT 'Active setup job associated with the request at submission time.',
MODIFY COLUMN work_center_name VARCHAR(64) NOT NULL COMMENT 'Work center that submitted the request.',
MODIFY COLUMN requester_employee_number VARCHAR(32) NOT NULL COMMENT 'Employee number of the requester.',
MODIFY COLUMN requester_employee_name VARCHAR(128) NOT NULL COMMENT 'Display name of the requester.',
MODIFY COLUMN status VARCHAR(32) NOT NULL DEFAULT 'Pending' COMMENT 'Lifecycle status, for example Pending, InProgress, Resolved, or Canceled.',
MODIFY COLUMN requested_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the request was submitted.',
MODIFY COLUMN target_time_utc DATETIME NULL COMMENT 'UTC target completion time when one is supplied.',
MODIFY COLUMN is_overdue TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True when the request passed its target time without resolution.',
MODIFY COLUMN assigned_material_handler VARCHAR(128) NULL COMMENT 'Material handler assigned to fulfil the request.',
MODIFY COLUMN cancellation_reason VARCHAR(255) NULL COMMENT 'Reason captured when the request is canceled.',
MODIFY COLUMN canceled_utc DATETIME NULL COMMENT 'UTC timestamp when the request was canceled.',
MODIFY COLUMN canceled_by_employee_number VARCHAR(32) NULL COMMENT 'Employee number of the person who canceled the request.',
MODIFY COLUMN note VARCHAR(500) NULL COMMENT 'Optional handler/requester note on the request.',
MODIFY COLUMN accepted_utc DATETIME NULL COMMENT 'UTC timestamp when a handler accepted/claimed the request.',
MODIFY COLUMN completed_utc DATETIME NULL COMMENT 'UTC timestamp when the request was completed.',
MODIFY COLUMN released_utc DATETIME NULL COMMENT 'UTC timestamp when a handler released the request back to open.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';

ALTER TABLE waitlist_requests_audit COMMENT = 'Immutable audit/history trail of waitlist request status transitions (never purged).';

ALTER TABLE waitlist_requests_audit
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the audit entry.',
MODIFY COLUMN request_public_id CHAR(36) NOT NULL COMMENT 'Waitlist request public_id this entry belongs to.',
MODIFY COLUMN from_status VARCHAR(32) NULL COMMENT 'Status the request transitioned from.',
MODIFY COLUMN to_status VARCHAR(32) NULL COMMENT 'Status the request transitioned to.',
MODIFY COLUMN event_type VARCHAR(32) NOT NULL COMMENT 'Lifecycle event (Created/Accepted/Completed/Canceled/Released).',
MODIFY COLUMN actor_employee_number VARCHAR(32) NULL COMMENT 'Employee number of the actor who caused the event.',
MODIFY COLUMN actor_employee_name VARCHAR(128) NULL COMMENT 'Display name of the actor who caused the event.',
MODIFY COLUMN details VARCHAR(255) NULL COMMENT 'Free-text detail (e.g. cancellation reason).',
MODIFY COLUMN occurred_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the event occurred.';

ALTER TABLE waitlist_defect_types COMMENT = 'Managed list of non-conforming (NCM) defect types referenced by Pickup NCM requests (edited from Module_Settings).';

ALTER TABLE waitlist_defect_types
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the defect type row.',
MODIFY COLUMN defect_name VARCHAR(100) NOT NULL COMMENT 'Display name of the NCM defect type (e.g. Scratch, Dent, Wrong Color).',
MODIFY COLUMN description VARCHAR(500) NULL COMMENT 'Optional short description shown in the editor/picker.',
MODIFY COLUMN sort_order INT NOT NULL DEFAULT 0 COMMENT 'Display order within the picker list.',
MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the defect type is offered to workers.',
MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the defect type.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the defect type.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the defect type row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the defect type row was last updated.';

ALTER TABLE waitlist_request_item_configs COMMENT = 'One row per catalogued request Item: flow, answer requirement, prompt, limits, options, page fields and allotted minutes. Behaviour as data.';

ALTER TABLE waitlist_request_item_configs
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for the configuration row.',
MODIFY COLUMN item VARCHAR(64) NOT NULL COMMENT 'Catalog item code (e.g. pickup-coil, deliver-wrong-flatstock, other). One row per Item.',
MODIFY COLUMN category VARCHAR(16) NOT NULL COMMENT 'The Category the Item belongs to: Pickup, Deliver, Assist or Other.',
MODIFY COLUMN control_flow VARCHAR(64) NOT NULL DEFAULT 'direct-to-confirmation' COMMENT 'How far the flow goes before confirmation: direct-to-confirmation or collect-input-then-confirm.',
MODIFY COLUMN requires_answer TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Whether the Details step asks for an answer for this Item.',
MODIFY COLUMN answer_value_type VARCHAR(16) NULL COMMENT 'enum or text where an answer is required; NULL otherwise.',
MODIFY COLUMN prompt_text VARCHAR(500) NULL COMMENT 'The question shown where an answer is required.',
MODIFY COLUMN min_length INT NOT NULL DEFAULT 0 COMMENT 'Minimum length of a text answer; ignored where none is required.',
MODIFY COLUMN max_length INT NOT NULL DEFAULT 200 COMMENT 'Maximum length of a text answer; ignored where none is required.',
MODIFY COLUMN options_json JSON NULL COMMENT 'Ordered options for an enumerated answer, where the options are fixed by configuration.',
MODIFY COLUMN detail_fields_json JSON NULL COMMENT 'The ordered fields the Item page shows: label, value_type, source, order, is_required.',
MODIFY COLUMN allotted_minutes INT NULL COMMENT 'The Item configured allotment in minutes. NULL means not configured, and the application then uses the labelled 15-minute default.',
MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last changed this configuration row. Audit only; not part of the read contract.',
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the configuration row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the configuration row was last updated.';

-- ============================================================
-- config_images_locations_history - feature 008-part-pictures
-- ============================================================
-- New table. The owning artifact is Database/Tables/33_config_images_locations_history/create.sql; the guarded
-- statement below carries the same shape so a store that ALREADY EXISTS gains the table when this maintenance
-- file is run, which is the only path that reaches such a store. FOREIGN_KEY_CHECKS is off for the creation
-- because the table carries two foreign keys, and the order in which the referenced tables were created is not
-- this file's to assume.
SET FOREIGN_KEY_CHECKS = 0;

SET
    @has_config_images_locations_history_table := (
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE
            table_schema = DATABASE()
            AND table_name = 'config_images_locations_history'
    );

SET
    @sql_stmt := IF(
        @has_config_images_locations_history_table = 0,
        'CREATE TABLE config_images_locations_history (id BIGINT NOT NULL AUTO_INCREMENT COMMENT ''Surrogate primary key.'', public_id CHAR(36) NOT NULL COMMENT ''Public UUID for external references.'', image_location_id BIGINT NULL COMMENT ''The picture row this change belongs to; NULL once that row is retired'', scope VARCHAR(16) NOT NULL COMMENT ''Copied at write time: request_item, request_category, work_center, visual_part, or wip_part'', scope_item_id VARCHAR(190) NOT NULL COMMENT ''Copied at write time: the item code, category code, work center id, or part number'', previous_image_path VARCHAR(500) NULL COMMENT ''The relative path this change replaced; NULL for a first picture'', new_image_path VARCHAR(500) NOT NULL COMMENT ''The relative path that replaced it'', changed_by_user_id BIGINT NULL COMMENT ''The actor who set or replaced the picture'', changed_utc DATETIME NOT NULL COMMENT ''UTC timestamp of the change'', PRIMARY KEY (id), UNIQUE KEY uq_config_images_locations_history_public_id (public_id), KEY idx_config_images_locations_history_scope_item (scope, scope_item_id, changed_utc) COMMENT ''Read one part''''s record newest first'', KEY idx_config_images_locations_history_image_location_id (image_location_id), KEY idx_config_images_locations_history_changed_by_user_id (changed_by_user_id), CONSTRAINT fk_config_images_locations_history_image_location_id FOREIGN KEY (image_location_id) REFERENCES config_images_locations (id) ON DELETE SET NULL, CONSTRAINT fk_config_images_locations_history_changed_by_user_id FOREIGN KEY (changed_by_user_id) REFERENCES core_users_profiles (id) ON DELETE SET NULL) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = ''Who set or replaced a stored picture, when, and what the previous picture was.''',
        'SELECT ''config_images_locations_history already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- auth_user_management_audit - feature 006
-- ============================================================
-- New table. The owning artifact is Database/Tables/32_auth_user_management_audit/create.sql; the guarded
-- statement below carries the same shape so a store that ALREADY EXISTS gains the table when this maintenance
-- file is run, which is the only path that reaches such a store. It carries no foreign keys, so it can be
-- created in any order and needs no FOREIGN_KEY_CHECKS handling.
SET
    @has_auth_user_management_audit_table := (
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE
            table_schema = DATABASE()
            AND table_name = 'auth_user_management_audit'
    );

SET
    @sql_stmt := IF(
        @has_auth_user_management_audit_table = 0,
        'CREATE TABLE auth_user_management_audit (id BIGINT NOT NULL AUTO_INCREMENT COMMENT ''Surrogate primary key.'', public_id CHAR(36) NOT NULL COMMENT ''Public UUID for external references.'', change_group_id CHAR(36) NOT NULL COMMENT ''Shared by every row one save produced, so one act reads as one act.'', target_user_id BIGINT NOT NULL COMMENT ''The person whose account changed.'', field_name VARCHAR(64) NOT NULL COMMENT ''The one field this row is about.'', previous_value TEXT NULL COMMENT ''The value before the change, or NULL where there is none to record.'', changed_value TEXT NULL COMMENT ''The value after the change, or NULL where there is none to record.'', actor_user_id BIGINT NULL COMMENT ''The acting person, joined for durability.'', actor_display_name VARCHAR(256) NOT NULL COMMENT ''The actor display name as it was when they acted.'', actor_employee_identifier VARCHAR(128) NOT NULL COMMENT ''The actor employee number as it was when they acted.'', actor_role_code VARCHAR(64) NOT NULL COMMENT ''The actor role code as it was when they acted.'', occurred_utc DATETIME NOT NULL COMMENT ''UTC timestamp when the change was recorded.'', PRIMARY KEY (id), UNIQUE KEY uq_auth_user_management_audit_public_id (public_id), KEY idx_auth_user_management_audit_change_group_id (change_group_id), KEY idx_audit_target_user_id_occurred_utc (target_user_id, occurred_utc)) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci',
        'SELECT ''auth_user_management_audit already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

-- ============================================================
-- Retired objects - feature 001-module-mock-visual-fallback (FR-014)
-- ============================================================
-- The database-backed demo/sample family was retired in full. These objects are no longer created
-- by an artifact in this tree and no longer carry descriptions here; the matching drop statements
-- remain as the rollback artifacts so a DBA can promote the removal:
--   Tables (drop via Database/Tables/<name>/rollback.sql):
--     mock_master_tables_registry, mock_parts, mock_work_orders, mock_work_centers, mock_locations,
--     mock_requesters, mock_inventory_locations, mock_request_types
--   Stored procedures (drop via Database/StoredProcedures/sp_mock_*/rollback.sql): the 33 sp_mock_* procedures
--   Seed: seed_mock_master_default (artifact removed with the tables it populated)

-- ============================================================
-- Retired objects - feature 004-unified-card-item-picker (FR-023)
-- ============================================================
-- A request carries a Category and an Item, and no longer a request type or a subtype (FR-004/FR-016/FR-023).
-- The type/subtype catalog is retired in full: these objects are no longer created by an artifact in this
-- tree and no longer carry descriptions here. The matching rollback artifacts remain, so a DBA can promote
-- the removal, and the mapping the two tables carried was consumed at design time into
-- waitlist_request_item_configs before they were removed (their populated category / item_id columns):
--   Tables (drop via Database/Tables/<name>/rollback.sql):
--     waitlist_request_types, waitlist_request_subtypes
--   Stored procedures (drop via Database/StoredProcedures/<name>/rollback.sql):
--     sp_waitlist_request_types_get, sp_waitlist_request_subtypes_get
--   Seed: seed_waitlist_request_catalog (artifact removed with the tables it populated), together with the
--     dead Database/Seeds/seed_waitlist_requests_default/migrate_subtypes.sql whose request_type / subtype
--     columns stopped existing when waitlist_requests_queue was re-keyed.
