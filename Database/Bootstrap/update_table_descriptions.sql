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

-- auth_sessions_tokens is deliberately absent here. The table was retired by feature 010 (task T060,
-- superseded by user_active_sessions) and its ALTER statements were removed when the table went: a MODIFY
-- against a table that no longer exists stops this script on the very first run after the deletion, which is
-- what happened on 2026-09-27 and left every description below it unapplied. The object itself is recorded in
-- the retired-objects section at the foot of this file, which is where a description for it now belongs.


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
MODIFY COLUMN module VARCHAR(64) NULL COMMENT 'The part of the application the entry came from; an ILogger category name lands here.',
MODIFY COLUMN error_type VARCHAR(128) NULL COMMENT 'The fault type, when there was a fault.',
MODIFY COLUMN exception_detail MEDIUMTEXT NULL COMMENT 'The serialized exception chain, one JSON node per exception, outermost first.',
MODIFY COLUMN error_fingerprint CHAR(64) NULL COMMENT 'SHA-256 of the fault shape, so the same fault groups together across machines; NULL without an exception.',
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

ALTER TABLE config_images_locations COMMENT = 'Image location overrides for request items, their Category families, work centers, parts and machines. Enables role-based customization of visual assets via cascade resolution pattern.';

ALTER TABLE config_images_locations
MODIFY COLUMN id BIGINT NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN public_id CHAR(36) NOT NULL COMMENT 'Public UUID for image location override row.',
MODIFY COLUMN scope VARCHAR(16) NOT NULL COMMENT 'Scope type: request_item, request_category, work_center, visual_part, wip_part, or computer.',
MODIFY COLUMN scope_item_id VARCHAR(190) NOT NULL COMMENT 'Identifier within scope: the Item or Category code, the numeric work center id, the part number, or <computer_id>:<source_kind> at computer scope.',
MODIFY COLUMN computer_id BIGINT NULL COMMENT 'At computer scope, the machine the row belongs to; NULL for the five picture scopes.',
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
-- user_active_sessions - feature 010-startup-rebuild (task T044)
-- ============================================================
-- New table. The owning artifact is Database/Tables/34_user_active_sessions/create.sql; the guarded statement
-- below carries the same shape so a store that ALREADY EXISTS gains the table when this maintenance file is run,
-- which is the only path that reaches such a store. FOREIGN_KEY_CHECKS is off for the creation because the table
-- carries two foreign keys, and the order in which the referenced tables were created is not this file's to
-- assume.
SET FOREIGN_KEY_CHECKS = 0;

SET
    @has_user_active_sessions_table := (
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE
            table_schema = DATABASE()
            AND table_name = 'user_active_sessions'
    );

SET
    @sql_stmt := IF(
        @has_user_active_sessions_table = 0,
        'CREATE TABLE user_active_sessions (id BIGINT NOT NULL AUTO_INCREMENT COMMENT ''Surrogate primary key.'', public_id CHAR(36) NOT NULL COMMENT ''Public UUID for external references.'', user_id BIGINT NOT NULL COMMENT ''The person the session belongs to.'', computer_id BIGINT NOT NULL COMMENT ''The machine the session was issued on.'', token_hash CHAR(64) NOT NULL COMMENT ''SHA-256 hex digest of the issued token; never the token itself'', token_salt VARBINARY(32) NOT NULL COMMENT ''Salt the digest was computed with; present whenever a digest is'', issued_utc DATETIME NOT NULL COMMENT ''When the token was issued, from the store clock'', expires_utc DATETIME NOT NULL COMMENT ''When the token stops being valid, from the store clock'', revoked_utc DATETIME NULL COMMENT ''When the session was cleared; NULL while it has not been'', is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''Soft state: 0 once cleared, so the row survives as evidence'', source_label VARCHAR(32) NOT NULL COMMENT ''Caller vocabulary for where the session came from'', created_utc DATETIME NOT NULL COMMENT ''When this row was first written; unchanged by a later sign-in'', PRIMARY KEY (id), UNIQUE KEY uq_user_active_sessions_public_id (public_id), UNIQUE KEY uq_user_active_sessions_user_computer (user_id, computer_id) COMMENT ''One active row per person per machine'', KEY idx_user_active_sessions_is_active_expires_utc (is_active, expires_utc), KEY idx_user_active_sessions_computer_id (computer_id), CONSTRAINT fk_user_active_sessions_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id), CONSTRAINT fk_user_active_sessions_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = ''The one active session for a person on a machine: a salted token digest, its lifetime and its machine key.''',
        'SELECT ''user_active_sessions already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- auth_remembered_sign_ins - feature 010-startup-rebuild (task T046, plan D12)
-- ============================================================
-- New table. The owning artifact is Database/Tables/35_auth_remembered_sign_ins/create.sql; the guarded statement
-- below carries the same shape so a store that ALREADY EXISTS gains the table when this maintenance file is run.
-- FOREIGN_KEY_CHECKS is off for the same reason as above: the table carries two foreign keys.
SET FOREIGN_KEY_CHECKS = 0;

SET
    @has_auth_remembered_sign_ins_table := (
        SELECT COUNT(*)
        FROM information_schema.tables
        WHERE
            table_schema = DATABASE()
            AND table_name = 'auth_remembered_sign_ins'
    );

SET
    @sql_stmt := IF(
        @has_auth_remembered_sign_ins_table = 0,
        'CREATE TABLE auth_remembered_sign_ins (id BIGINT NOT NULL AUTO_INCREMENT COMMENT ''Surrogate primary key.'', public_id CHAR(36) NOT NULL COMMENT ''Public UUID for external references.'', user_id BIGINT NOT NULL COMMENT ''The person the remembered sign-in belongs to.'', computer_id BIGINT NOT NULL COMMENT ''The machine the remembered sign-in was made on.'', payload_ciphertext VARBINARY(512) NOT NULL COMMENT ''The encrypted remembered payload; opaque to the store'', payload_iv VARBINARY(16) NOT NULL COMMENT ''The initialisation vector the ciphertext was produced with'', key_fingerprint CHAR(64) NOT NULL COMMENT ''Identifies the key that encrypted the payload, so a rotated key is detected rather than raised'', is_active TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''Soft state: 0 once cleared, and a cleared row is never read'', created_utc DATETIME NOT NULL COMMENT ''When this person first used this machine to remember a sign-in'', updated_utc DATETIME NOT NULL COMMENT ''When the payload was last written or cleared'', PRIMARY KEY (id), UNIQUE KEY uq_auth_remembered_sign_ins_public_id (public_id), UNIQUE KEY uq_auth_remembered_sign_ins_user_computer (user_id, computer_id) COMMENT ''One remembered sign-in per person per machine'', CONSTRAINT fk_auth_remembered_sign_ins_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id), CONSTRAINT fk_auth_remembered_sign_ins_core_computers_registry_computer_id FOREIGN KEY (computer_id) REFERENCES core_computers_registry (id)) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = ''An encrypted remembered sign-in per person and machine, with the initialisation vector and the fingerprint of the key that produced it.''',
        'SELECT ''auth_remembered_sign_ins already exists'''
    );

PREPARE stmt FROM @sql_stmt;

EXECUTE stmt;

DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- sp_auth_user_credential_get - feature 010-startup-rebuild (task T110)
-- ============================================================
-- New procedure. It creates no table and adds no column, so there is nothing to ALTER and no description to
-- attach: the read is described where a procedure is described, in its own artifact and in the aggregate's
-- header. What is recorded here is that the artifact was added, which is what the ruleset requires of every SQL
-- artifact under Database/.
--   Owning artifact: Database/StoredProcedures/sp_auth_user_credential_get/create.sql, with its rollback beside
--     it, and its body registered in Database/StoredProcedures/AllSPs.sql.
--   What it reads: one active account's identity and role in force in `core_users_profiles`, joined to
--     `auth_roles_assignments` and `auth_roles_catalog` exactly as `sp_auth_user_row_get` reads them, plus the
--     four columns that read deliberately leaves out: `password_hash`, `password_salt`,
--     `require_password_change` and `temporary_credential_failed_attempts`. It owns no data and drops cleanly.
--   Why it exists: the retired `sp_auth_credentials_check` (T020) was the only read that returned credential
--     material, so the rebuilt credential check and the machine-setup gate had nothing to call and both failed
--     closed. This is that read, restored under a name that says what it returns, shared by both callers so the
--     two cannot drift into different questions. `require_password_change` and the attempt count travel with the
--     identity because the five-attempt limit applies to temporary credentials alone (FR-012, SC-007).

-- ============================================================
-- sp_auth_user_password_set - feature 010-startup-rebuild (task T116)
-- ============================================================
-- New procedure. It creates no table and adds no column, so there is nothing to ALTER and no description to
-- attach: the write is described where a procedure is described, in its own artifact and in the aggregate's
-- header. What is recorded here is that the artifact was added, which is what the ruleset requires of every SQL
-- artifact under Database/.
--   Owning artifact: Database/StoredProcedures/sp_auth_user_password_set/create.sql, with its rollback beside it,
--     and its body registered in Database/StoredProcedures/AllSPs.sql.
--   What it writes: three columns of one `core_users_profiles` row — `password_hash`, `password_salt`,
--     `require_password_change` set to 0 — plus `temporary_credential_failed_attempts` set to 0. It touches no
--     other row, no other table and no session, and it writes only while it runs, so its rollback cannot lose
--     data.
--   Why it exists: FR-013 requires a person still on a temporary credential to set a new password before
--     anything else. The retired `sp_auth_user_password_update` (T020) was the only write of that column pair
--     that an account could make for itself, and nothing that survives replaces it:
--     `sp_user_management_update` deliberately leaves credentials alone, and `sp_user_management_reset_password`
--     issues an administrator's one-time PIN and would set `require_password_change` back to 1 rather than clear
--     it. Clearing that flag is what takes the account out of the temporary state the five-attempt limit is
--     judged against (FR-012).

-- ============================================================
-- sp_auth_user_roles_get - feature 010-startup-rebuild (task T195)
-- ============================================================
-- New procedure. It creates no table and adds no column, so there is nothing to ALTER and no description to
-- attach: the read is described where a procedure is described, in its own artifact and in the aggregate's
-- header. What is recorded here is that the artifact was added, which is what the ruleset requires of every SQL
-- artifact under Database/.
--   Owning artifact: Database/StoredProcedures/sp_auth_user_roles_get/create.sql, with its rollback beside it,
--     and its body registered in Database/StoredProcedures/AllSPs.sql.
--   What it reads: the existing auth_roles_assignments joined to auth_roles_catalog for one active person, one
--     row per assignment. It owns no data and it drops cleanly.
--   Why it exists: it closes the held-roles gap on the identity contract. sp_auth_user_row_get resolves the
--     person and the single role in force; this read returns every assignment, so IPersonIdentity.HeldRoleCodes
--     is the person's whole set rather than the one role the logon read happens to return.

-- ============================================================
-- sp_config_images_locations_computer_sources_all_get - retired by task T198 (FR-040)
-- ============================================================
-- Withdrawn, so it no longer carries a description of what it does. Its create.sql is gone — no artifact in this
-- tree creates it any more — its body was removed from Database/StoredProcedures/AllSPs.sql, and the rollback
-- beside the folder is the drop that remains. Its only caller was the machine-configuration service's
-- picture-source read, which T230 removed with the per-computer folders themselves, so the question it existed to
-- answer ("never given its folders" apart from "its folders were removed") is no longer one the application asks.
-- The withdrawal is listed with the other two computer-source artifacts in the retired-objects section below.

-- ============================================================
-- sp_core_computers_registry_display_name_get - feature 010-startup-rebuild (task T075)
-- ============================================================
-- New procedure. It creates no table and adds no column, so there is nothing to ALTER and no description to
-- attach: the read is described where a procedure is described, in its own artifact and in the aggregate's
-- header. What is recorded here is that the artifact was added, which is what the ruleset requires of every SQL
-- artifact under Database/.
--   Owning artifact: Database/StoredProcedures/sp_core_computers_registry_display_name_get/create.sql, with its
--     rollback beside it, and its body registered in Database/StoredProcedures/AllSPs.sql.
--   What it reads: the one row `uq_core_computers_registry_display_name` allows to hold a given display name.
--   Why it exists: a machine save must ask for a different display name when another machine already holds the
--     one it was given (the contract's edge case), and the same read lets a reset refuse before it writes when
--     the default name it would restore is taken. Both are answers the screen acts on rather than exceptions.

-- ============================================================
-- sp_machine_configuration_reset - feature 010-startup-rebuild (task T075)
-- ============================================================
-- New procedure. It creates no table and adds no column, so there is nothing to ALTER and no description to
-- attach: the write is described where a procedure is described, in its own artifact and in the aggregate's
-- header. What is recorded here is that the artifact was added, which is what the ruleset requires of every SQL
-- artifact under Database/.
--   Owning artifact: Database/StoredProcedures/sp_machine_configuration_reset/create.sql, with its rollback
--     beside it, and its body registered in Database/StoredProcedures/AllSPs.sql.
--   What it writes: this machine's own configuration and nothing else — its display name and description in
--     `core_computers_registry`, its picture sources in `config_images_locations` (withdrawn, not deleted), and
--     its scoped preference rows in `config_settings_values`. All three writes are one transaction behind one
--     call, because a half-finished reset leaves a machine in a state none of the three tables can express.
--   Why it exists: FR-018 requires a targeted restore of exactly the parts named as broken, on this machine
--     only, with a single implementation that the recovery step drives rather than re-implementing. It never
--     touches a person, a role, a permission, another machine, a session or a log entry, and it deliberately
--     does not delete the registry row: that row is referenced by `user_active_sessions` and
--     `auth_remembered_sign_ins`, and neither may be cleared by a machine-configuration reset.

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

-- ============================================================
-- Retired objects - feature 010-startup-rebuild (tasks T020, T023, T061)
-- ============================================================
-- The old startup surface was removed and is being rebuilt, so the procedures only that surface used are no
-- longer created by an artifact in this tree and no longer carry descriptions here. The matching rollback
-- artifacts remain as the drop statements, so a DBA can promote the removal, and each procedure's body was
-- removed from Database/StoredProcedures/AllSPs.sql in the same change:
--   Stored procedures (drop via Database/StoredProcedures/<name>/rollback.sql):
--     sp_server_utc_now_get (the wrapper only; `fn_server_utc_now` is retained),
--     sp_auth_credentials_check, sp_auth_user_password_update, sp_auth_computer_registered_get,
--     sp_auth_session_expiry_get, sp_auth_password_reset_required_get
--   Validation: Database/Validation/startup_schema/validate.sql was reworked rather than deleted (T021). It
--     keeps the checks that cover shared tables and the retained `fn_server_utc_now`; the checks that covered
--     the retired session table (auth_sessions_tokens) were removed with the surface that used it.
--   Not retired, and deliberately so (T022): sp_core_computers_registry_lookup_by_name_mac_get,
--     sp_core_computers_registry_lookup_by_mac_get and sp_core_computers_registry_update_by_mac. They were
--     listed with the startup-only set, but a surviving artifact still names each of them — the rebuilt
--     machine-facts contract reads a machine by its (name, hardware address) pair, and the retained computer
--     registry service still calls all three — so removing them here would leave live code calling a
--     procedure no install creates. They retire with that service instead (T187).
--   Retained, unchanged: `fn_server_utc_now`. The rebuilt session, remembered sign-in, log and
--     picture-source procedures all compare against the store's own clock through it, so it stays the single
--     implementation of "now" (S8.1).
--   T061 then retired three more objects that the dead-weight audit (T056) found orphaned of application code.
--     They were not reached only by the old startup surface; their only consumers were the development seed and
--     this feature's own settings validator, and both were edited in the same change (T062), so nothing writes
--     or asserts them any more. Their rollback artifacts remain as the drop statements:
--     Tables (drop via Database/Tables/<name>/rollback.sql):
--       core_buildings_catalog, core_buildings_history
--     Stored procedures (drop via Database/StoredProcedures/<name>/rollback.sql):
--       sp_core_buildings_upsert
--     Seed: Database/Seeds/seed_dev_masked_baseline no longer truncates or inserts into core_buildings_catalog,
--       and its mirror block in Database/Seeds/AllSeeds.sql was removed with it.
--     Validation: Database/Validation/settings_schema/validate.sql no longer asserts the three objects exist. It
--       was reworked rather than deleted, keeping its checks on config_settings_values, config_settings_history,
--       fn_config_settings_scope_rank, sp_config_settings_get_effective, sp_config_settings_upsert and
--       vw_config_settings_scope_catalog.
--   The picture-layout seed was then retired with the helper only it deployed: the seed folder
--     Database/Seeds/seed_picture_layout_move (create and rollback removed together, so no artifact creates it and
--     none reverses it), its block removed from Database/Seeds/AllSeeds.sql, and its helper procedure
--     sp_seed_picture_layout_move dropped from the store rather than left behind — the seed created it and neither
--     the seed nor its rollback ever dropped it, so the drop is the store side of the retirement.
--     Why it goes: the application defaults out on first use, so the migration is not needed to reach a working
--     default. A scope with no row resolves to the application's own no-image path
--     (MTM_Waitlist.Settings/Models/ImageLocationDefaults.cs), and the only writers of config_images_locations
--     rows are the picture screen (sp_config_images_locations_insert) and
--     sp_config_images_locations_computer_sources_set — no seed inserts a row — so the move only ever rewrote rows
--     an operator or the computer-sources write had already put there.
--     Why nothing breaks: a store with no rows resolves to the default, and a row whose file is absent falls back
--     to the default asset (ImageLocationService.ResolveExistingPathAsync), so removing the seed falls back to the
--     application's own defaults rather than to nothing.
--     Retained, not retired: sp_config_images_locations_paths_move with its rollback, which the hand-run move
--     (tools/Move-PartPictureLayout.ps1) still uses, and Database/Validation/part_pictures_schema/validate.sql,
--     which still asserts that pair exists.
--   T198 then retired the three per-computer picture-source procedures, because the feature withdrew their owner:
--     where a machine's pictures come from became a plant-wide answer, so no computer captures a folder of its own
--     (FR-040, FR-041). Their folders are kept and their create.sql files are gone — the matching rollback is the
--     drop a DBA promotes — and their bodies were removed from Database/StoredProcedures/AllSPs.sql in the same
--     change:
--     Stored procedures (drop via Database/StoredProcedures/<name>/rollback.sql):
--       sp_config_images_locations_computer_sources_all_get (T075; its only caller was the machine-configuration
--         service's picture-source read, which T230 removed),
--       sp_config_images_locations_computer_sources_get (T051; the machine-setup screen stopped capturing folders
--         in T199 and the service stopped reading them in T230),
--       sp_config_images_locations_computer_sources_set (T051; the only writer of a `computer` scope row, which
--         T197 stopped the save writing and T199 took off the screen)
--     Seed: Database/Seeds/seed_retire_computer_scope_picture_sources is added rather than retired. It withdraws
--       the `computer` scope rows an existing store holds, so a computer set up before this change reads from the
--       plant-wide setting afterwards (SC-021), and its mirror block is in Database/Seeds/AllSeeds.sql.
--     Why they go now rather than earlier: the removal proof recorded `_get` and `_set` as consumerless but
--       deliberately kept, because the consumers were unbuilt. Those consumers were then built and withdrawn by
--       FR-040 inside the same feature, so the reason to keep them expired with the requirement.
--     Not retired: `config_images_locations` itself, its `computer` scope value and its `computer_id` column. A
--       store that already holds rows keeps them, withdrawn, and the table's own rollback is what removes them.
--   The development seed's credential was repaired, and seed application was taken out of the validator's
--     default path.
--     `Database/Seeds/seed_dev_masked_baseline` wrote the retired four-character marker `'0000'` into
--     `password_hash` with a null `password_salt`, and `PasswordSecretHasher.Verify` refuses a null salt before
--     it derives anything, so no comparison could confirm any of the ten seeded accounts: nothing could sign in
--     on a fresh install, at the machine-setup gate or at the sign-in screen, and nothing in the build said so.
--     Both files were changed together, because the aggregate is what the installer runs: the seed artifact and
--     its mirror block in `Database/Seeds/AllSeeds.sql`. Each account now carries `UNHEX(<16-byte salt>)` and the
--     base64 digest of the same development credential, produced by `PasswordSecretHasher.Hash` under its shipped
--     parameters (PBKDF2/SHA-256, 100000 iterations, a 32-byte hash). Every seeded account shares that value on
--     purpose: it is a development placeholder rather than a secret, and it belongs to no real person. Roles,
--     identifiers, `is_active` and `require_password_change = 1` are unchanged, so the credential is still a
--     temporary one and the person is asked to choose a real password at first sign-in.
--     Guarded by MTM_Waitlist.Tests/Module_Core/Services/DevelopmentSeedCredentialTests.cs, which reads this seed
--     file and confirms every row with the application's own comparison.
--     The reason this is recorded here and not only in the seed: `.github/scripts/validate-database-schema.ps1`
--     applied every seed file whenever the schema validated, and this seed truncates and rewrites
--     `core_users_profiles`, `auth_roles_catalog`, `auth_roles_assignments`, `config_settings_values` and
--     `core_computers_registry`. A validation run therefore rewrote the accounts of whatever store its
--     connection strings named, which in CI are secrets pointing at a real one. Seed application now happens
--     only when the run is given `-ApplySeeds`, so checking a schema no longer changes a store's data.
--     Correction to an earlier note in this block: the semicolon in the comment that used to sit here was NOT
--     what made the validator fail. Its statement splitter masks comments and quoted literals before looking for
--     delimiters, so that semicolon was already harmless. Removing it is a tidy-up, not a fix.
