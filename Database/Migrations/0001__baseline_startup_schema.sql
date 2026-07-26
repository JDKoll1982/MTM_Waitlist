-- MTM_Waitlist startup baseline schema
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

CREATE TABLE IF NOT EXISTS core_users_profiles (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    username_normalized VARCHAR(128) NOT NULL,
    display_name VARCHAR(256) NOT NULL,
    employee_identifier VARCHAR(128) NULL,
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_users_profiles_public_id (public_id),
    UNIQUE KEY uq_core_users_profiles_username_normalized (username_normalized)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

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

CREATE TABLE IF NOT EXISTS core_workstations_registry (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    workstation_name VARCHAR(128) NOT NULL,
    hostname_normalized VARCHAR(255) NOT NULL,
    mac_address_normalized VARCHAR(64) NOT NULL,
    is_registered TINYINT(1) NOT NULL DEFAULT 1,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_core_workstations_registry_public_id (public_id),
    UNIQUE KEY uq_core_workstations_registry_hostname_mac_address (
        hostname_normalized,
        mac_address_normalized
    )
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS auth_sessions_tokens (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    user_id BIGINT NOT NULL,
    workstation_id BIGINT NULL,
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
    KEY idx_auth_sessions_tokens_workstation_id_expires_utc (workstation_id, expires_utc),
    KEY idx_auth_sessions_tokens_is_active_expires_utc (is_active, expires_utc),
    CONSTRAINT fk_auth_sessions_tokens_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id),
    CONSTRAINT fk_sessions_tokens_workstations_workstation_id FOREIGN KEY (workstation_id) REFERENCES core_workstations_registry (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS config_settings_values (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    setting_key VARCHAR(190) NOT NULL,
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
    UNIQUE KEY uq_config_settings_values_setting_key (setting_key),
    KEY idx_config_settings_values_updated_by_user_id (updated_by_user_id),
    CONSTRAINT fk_config_settings_values_core_users_profiles_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS config_settings_history (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    config_setting_id BIGINT NOT NULL,
    setting_key VARCHAR(190) NOT NULL,
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
    CONSTRAINT fk_settings_history_settings_values_config_setting_id FOREIGN KEY (config_setting_id) REFERENCES config_settings_values (id),
    CONSTRAINT fk_settings_history_users_changed_by_user_id FOREIGN KEY (changed_by_user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

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