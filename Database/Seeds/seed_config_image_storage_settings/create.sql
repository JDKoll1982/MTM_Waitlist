-- Seed: Image Storage Configuration Overrides
-- Description: Populates config_settings_values with default image storage settings
--              that can be overridden by admins in-app.
-- Table: config_settings_values
-- Scope: global (all_users)
--
-- It UPSERTS and never truncates, and AllSeeds.sql places it after the settings baseline on purpose: that
-- baseline TRUNCATEs config_settings_values, so these rows written any earlier are thrown away. That is how a
-- freshly installed store ended up with none of them (defect fixed 2026-11-15). Upserting rather than
-- truncating keeps it re-runnable without removing a setting another seed owns.

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
        '\\\\mtmanu-fs01\\Expo Drive\\MH_RESOURCE\\Material_Handler\\MTM Applications\\MTM Waitlist Application\\Images',
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