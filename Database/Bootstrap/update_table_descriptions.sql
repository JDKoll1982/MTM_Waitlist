-- Update script: apply table descriptions for mtm_waitlist
-- Engine: MySQL 5.7

USE mtm_waitlist;

ALTER TABLE core_users_profiles
    COMMENT = 'User profile and authentication identity records.';

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

ALTER TABLE core_workstations_registry
    COMMENT = 'Registered workstation and host identity catalog.';

ALTER TABLE core_workstations_registry
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for workstation record.',
    MODIFY COLUMN workstation_name VARCHAR(128) NOT NULL COMMENT 'Friendly workstation/computer name.',
    MODIFY COLUMN hostname_normalized VARCHAR(255) NOT NULL COMMENT 'Normalized host name used for identity matching.',
    MODIFY COLUMN mac_address_normalized VARCHAR(64) NOT NULL COMMENT 'Normalized MAC address for workstation identity.',
    MODIFY COLUMN is_registered TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the workstation is currently registered.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';

ALTER TABLE auth_roles_catalog
    COMMENT = 'Role definitions used for RBAC authorization.';

ALTER TABLE auth_roles_catalog
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for role record.',
    MODIFY COLUMN role_code VARCHAR(64) NOT NULL COMMENT 'Machine-friendly unique role code.',
    MODIFY COLUMN role_name VARCHAR(128) NOT NULL COMMENT 'Human-readable role name.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.';

ALTER TABLE auth_roles_assignments
    COMMENT = 'User-to-role assignment records for RBAC.';

ALTER TABLE auth_roles_assignments
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for assignment record.',
    MODIFY COLUMN user_id BIGINT NOT NULL COMMENT 'Foreign key to core_users_profiles.id.',
    MODIFY COLUMN role_id BIGINT NOT NULL COMMENT 'Foreign key to auth_roles_catalog.id.',
    MODIFY COLUMN assigned_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the role was assigned.',
    MODIFY COLUMN assigned_by_user_id BIGINT NULL COMMENT 'User who assigned the role.';

ALTER TABLE auth_sessions_tokens
    COMMENT = 'Session token hash metadata and lifecycle state.';

ALTER TABLE auth_sessions_tokens
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for session record.',
    MODIFY COLUMN user_id BIGINT NOT NULL COMMENT 'Foreign key to core_users_profiles.id.',
    MODIFY COLUMN workstation_id BIGINT NULL COMMENT 'Optional foreign key to core_workstations_registry.id.',
    MODIFY COLUMN token_hash CHAR(64) NOT NULL COMMENT 'Hashed session token value.',
    MODIFY COLUMN token_salt VARBINARY(32) NOT NULL COMMENT 'Salt used for token hashing.',
    MODIFY COLUMN token_version SMALLINT NOT NULL DEFAULT 1 COMMENT 'Token schema/hash version.',
    MODIFY COLUMN issued_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the token was issued.',
    MODIFY COLUMN expires_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the token expires.',
    MODIFY COLUMN revoked_utc DATETIME NULL COMMENT 'UTC timestamp when the token was revoked.',
    MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the token is currently active.',
    MODIFY COLUMN source_label VARCHAR(32) NOT NULL COMMENT 'Token source label such as startup or login.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.';

ALTER TABLE core_buildings_catalog
    COMMENT = 'Active building and facility catalog.';

ALTER TABLE core_buildings_catalog
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for building record.',
    MODIFY COLUMN building_code VARCHAR(64) NOT NULL COMMENT 'Unique building code.',
    MODIFY COLUMN building_name VARCHAR(128) NOT NULL COMMENT 'Display name for building/facility.',
    MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the building is active.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was created.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when the row was last updated.',
    MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the building record.';

ALTER TABLE core_buildings_history
    COMMENT = 'Audit history for building catalog changes.';

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

ALTER TABLE config_settings_values
    COMMENT = 'Current effective configuration setting values by scope.';

ALTER TABLE config_settings_values
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for setting row.',
    MODIFY COLUMN setting_key VARCHAR(190) NOT NULL COMMENT 'Unique setting identifier key.',
    MODIFY COLUMN scope_type VARCHAR(16) NOT NULL DEFAULT 'all_users' COMMENT 'Scope kind such as workstation, user, or all_users.',
    MODIFY COLUMN scope_key VARCHAR(255) NOT NULL DEFAULT 'all_users' COMMENT 'Scope discriminator key value.',
    MODIFY COLUMN workstation_id BIGINT NULL COMMENT 'Optional workstation scope foreign key.',
    MODIFY COLUMN user_id BIGINT NULL COMMENT 'Optional user scope foreign key.',
    MODIFY COLUMN setting_value TEXT NULL COMMENT 'Text setting value payload.',
    MODIFY COLUMN setting_value_int BIGINT NULL COMMENT 'Integer setting value payload.',
    MODIFY COLUMN setting_value_bool TINYINT(1) NULL COMMENT 'Boolean setting value payload.',
    MODIFY COLUMN setting_value_decimal DECIMAL(18, 6) NULL COMMENT 'Decimal setting value payload.',
    MODIFY COLUMN setting_value_datetime_utc DATETIME NULL COMMENT 'Datetime setting value payload in UTC.',
    MODIFY COLUMN value_type VARCHAR(32) NOT NULL COMMENT 'Declared value type for the setting payload.',
    MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the setting.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when setting was last updated.';

ALTER TABLE config_settings_history
    COMMENT = 'Audit trail for configuration setting changes.';

ALTER TABLE config_settings_history
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for history row.',
    MODIFY COLUMN config_setting_id BIGINT NOT NULL COMMENT 'Foreign key to config_settings_values.id.',
    MODIFY COLUMN setting_key VARCHAR(190) NOT NULL COMMENT 'Setting key snapshot at time of change.',
    MODIFY COLUMN scope_type VARCHAR(16) NOT NULL COMMENT 'Scope type snapshot at time of change.',
    MODIFY COLUMN scope_key VARCHAR(255) NOT NULL COMMENT 'Scope key snapshot at time of change.',
    MODIFY COLUMN workstation_id BIGINT NULL COMMENT 'Optional workstation scope foreign key snapshot.',
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

ALTER TABLE ops_startup_logs
    COMMENT = 'Startup and operational telemetry log entries.';

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
    MODIFY COLUMN host_id VARCHAR(128) NULL COMMENT 'Host/workstation identifier captured for event.',
    MODIFY COLUMN mac_address VARCHAR(64) NULL COMMENT 'MAC address captured for event.',
    MODIFY COLUMN message TEXT NOT NULL COMMENT 'Primary log message text.',
    MODIFY COLUMN payload_json MEDIUMTEXT NULL COMMENT 'Optional structured payload as JSON text.',
    MODIFY COLUMN previous_hash CHAR(64) NULL COMMENT 'Previous entry hash for chain validation.',
    MODIFY COLUMN entry_hash CHAR(64) NOT NULL COMMENT 'Hash for current log entry integrity.';

ALTER TABLE setup_active_jobs
    COMMENT = 'Current active setup job assignments by work center.';

ALTER TABLE setup_active_jobs
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for active job row.',
    MODIFY COLUMN work_order VARCHAR(32) NOT NULL COMMENT 'Work order number.',
    MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number for the setup job.',
    MODIFY COLUMN sequence_number VARCHAR(32) NOT NULL COMMENT 'Sequence number for the setup job.',
    MODIFY COLUMN work_center VARCHAR(64) NOT NULL COMMENT 'Work center or press identifier.',
    MODIFY COLUMN selected_dunnage_type_id VARCHAR(64) NULL COMMENT 'Selected dunnage type identifier.',
    MODIFY COLUMN selected_dunnage_part_id VARCHAR(64) NULL COMMENT 'Selected dunnage part identifier.',
    MODIFY COLUMN subordinate_parts_json JSON NULL COMMENT 'Serialized subordinate parts payload.',
    MODIFY COLUMN selected_dunnage_parts_json JSON NULL COMMENT 'Serialized selected dunnage parts payload.',
    MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether the active job row is active.',
    MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the active job row.',
    MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the active job row.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE setup_job_history
    COMMENT = 'Historical setup job events and saved setup snapshots.';

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
    MODIFY COLUMN subordinate_parts_json JSON NULL COMMENT 'Serialized subordinate parts snapshot.',
    MODIFY COLUMN selected_dunnage_parts_json JSON NULL COMMENT 'Serialized selected dunnage parts snapshot.',
    MODIFY COLUMN changed_by_user_id BIGINT NULL COMMENT 'User who caused the history event.',
    MODIFY COLUMN changed_utc DATETIME NOT NULL COMMENT 'UTC timestamp when history row was recorded.';

ALTER TABLE setup_workstations_catalog
    COMMENT = 'Setup work center catalog used by setup and waitlist flows.';

ALTER TABLE setup_workstations_catalog
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for work center catalog row.',
    MODIFY COLUMN workstation_name VARCHAR(64) NOT NULL COMMENT 'Work center or press display name.',
    MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether this work center is active.',
    MODIFY COLUMN sort_rank INT NOT NULL DEFAULT 100 COMMENT 'Sort order rank for UI lists.',
    MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the work center row.',
    MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the work center row.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';

ALTER TABLE config_workstation_hot_workcenters
    COMMENT = 'Per-computer hot work center preferences and ordering.';

ALTER TABLE config_workstation_hot_workcenters
    MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
    MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for hot mapping row.',
    MODIFY COLUMN core_workstation_id BIGINT NOT NULL COMMENT 'Foreign key to core_workstations_registry.id.',
    MODIFY COLUMN setup_workstation_id BIGINT NOT NULL COMMENT 'Foreign key to setup_workstations_catalog.id.',
    MODIFY COLUMN sort_rank INT NOT NULL DEFAULT 100 COMMENT 'Display order for hot work centers per workstation.',
    MODIFY COLUMN is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT 'Whether this hot mapping is active.',
    MODIFY COLUMN created_by_user_id BIGINT NULL COMMENT 'User who created the mapping row.',
    MODIFY COLUMN updated_by_user_id BIGINT NULL COMMENT 'User who last updated the mapping row.',
    MODIFY COLUMN created_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was created.',
    MODIFY COLUMN updated_utc DATETIME NOT NULL COMMENT 'UTC timestamp when row was last updated.';
