-- Seed: Image Storage Configuration Overrides
-- Description: Populates config_settings_values with default image storage settings
--              that can be overridden by admins in-app.
-- Table: config_settings_values
-- Scope: global (all_users)

USE mtm_waitlist;

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES (
        UUID(),
        'image_storage.shared_folder_path',
        'all_users',
        'all_users',
        'X:\\Software Development\\Live Applications\\MTM_Waitlist\\Images',
        'text',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'image_storage.max_file_size_bytes',
        'all_users',
        'all_users',
        NULL,
        'int',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'image_storage.enable_archive_versioning',
        'all_users',
        'all_users',
        NULL,
        'bool',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'image_storage.archive_keep_days',
        'all_users',
        'all_users',
        NULL,
        'int',
        NULL,
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    scope_type = VALUES(scope_type),
    scope_key = VALUES(scope_key),
    setting_value = VALUES(setting_value),
    setting_value_int = VALUES(setting_value_int),
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);