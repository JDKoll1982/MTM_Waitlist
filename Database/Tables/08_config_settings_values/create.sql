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