-- Rollback for dev/test masked baseline seed
-- Remove the synthetic startup seed records if they exist.

USE mtm_waitlist;

DELETE FROM auth_roles_assignments
WHERE
    assigned_by_user_id IS NOT NULL;

DELETE FROM core_buildings_catalog
WHERE
    building_code IN ('expo_drive', 'vits_drive');

DELETE FROM core_workstations_registry
WHERE
    workstation_name = 'johnspc';

DELETE FROM core_users_profiles WHERE username_normalized = 'johnk';

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
        'developer'
    );