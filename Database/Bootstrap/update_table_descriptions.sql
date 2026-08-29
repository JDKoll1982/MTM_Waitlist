-- Update script: apply table descriptions for mtm_waitlist
-- Engine: MySQL 5.7

USE mtm_waitlist;

ALTER TABLE core_users_profiles COMMENT = 'User profile and authentication identity records.';

ALTER TABLE core_users_profiles
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for external references.',
MODIFY COLUMN username_normalized VARCHAR(128) NOT NULL COMMENT 'Normalized unique username for sign-in lookup.',
MODIFY COLUMN password_hash VARCHAR(128) NOT NULL DEFAULT '0000' COMMENT 'Password hash value for authentication.',
MODIFY COLUMN password_salt VARBINARY(32) NULL COMMENT 'Per-user password salt.',
MODIFY COLUMN require_password_change TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Flag requiring password reset on next login.',
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

ALTER TABLE auth_roles_catalog
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for role record.',
MODIFY COLUMN role_code VARCHAR(64) NOT NULL COMMENT 'Machine-friendly unique role code.',
MODIFY COLUMN role_name VARCHAR(128) NOT NULL COMMENT 'Human-readable role name.',
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

ALTER TABLE config_images_locations COMMENT = 'Image path overrides for request types, work centers, and request subtypes. Enables role-based customization of visual assets via cascade resolution pattern.';

ALTER TABLE config_images_locations
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for image location override row.',
MODIFY COLUMN scope VARCHAR(16) NOT NULL COMMENT 'Scope type: request_type, work_center, or request_subtype.',
MODIFY COLUMN scope_item_id VARCHAR(190) NOT NULL COMMENT 'Stable identifier within scope. GUID for request types and subtypes, BIGINT for work centers.',
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
MODIFY COLUMN request_type VARCHAR(64) NOT NULL COMMENT 'Top-level request type display label.',
MODIFY COLUMN subtype VARCHAR(64) NULL COMMENT 'Request subtype display label; null when the type has no subtypes.',
MODIFY COLUMN input_value VARCHAR(255) NULL COMMENT 'Free-text value captured for request types that require input.',
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
MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';