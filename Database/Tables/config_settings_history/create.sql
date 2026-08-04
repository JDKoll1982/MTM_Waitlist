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
    workstation_id BIGINT NULL,
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
    CONSTRAINT fk_config_settings_history_core_workstations_registry_workstation_id FOREIGN KEY (workstation_id) REFERENCES core_workstations_registry (id),
    CONSTRAINT fk_config_settings_history_core_users_profiles_user_id FOREIGN KEY (user_id) REFERENCES core_users_profiles (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;