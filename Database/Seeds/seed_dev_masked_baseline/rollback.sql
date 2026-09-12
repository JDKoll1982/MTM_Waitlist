-- Rollback for dev/test masked baseline seed
-- Remove the synthetic startup seed records if they exist.

USE mtm_waitlist;

DELETE FROM auth_roles_assignments
WHERE
    assigned_by_user_id IS NOT NULL;

-- The per-user-type test accounts (T157) carry their own assignment rows.
DELETE FROM auth_roles_assignments
WHERE
    user_id IN (
        SELECT id FROM core_users_profiles
        WHERE username_normalized IN (
            'test.admin',
            'test.developer',
            'test.plant.manager',
            'test.setup.lead',
            'test.production.lead',
            'test.setup',
            'test.production',
            'test.material.handler'
        )
    );

DELETE FROM core_buildings_catalog
WHERE
    building_code IN ('expo_drive', 'vits_drive');

DELETE FROM core_computers_registry
WHERE
    computer_name = 'johnspc';

DELETE FROM core_users_profiles WHERE username_normalized = 'johnk';

DELETE FROM core_users_profiles
WHERE
    username_normalized IN (
        'test.admin',
        'test.developer',
        'test.plant.manager',
        'test.setup.lead',
        'test.production.lead',
        'test.setup',
        'test.production',
        'test.material.handler'
    );

DELETE FROM config_settings_values
WHERE
    setting_key IN (
        'sessions.retention_inactive_days',
        'waitlist.resolved_retention_days',
        'settings.history_retention_days'
    );

DELETE FROM auth_roles_catalog
WHERE
    role_code IN (
        'material_handler',
        'production',
        'production_lead',
        'setup',
        'setup_lead',
        'plant_manager',
        'developer',
        'admin'
    );