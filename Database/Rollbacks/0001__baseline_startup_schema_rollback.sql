-- Rollback for baseline startup schema

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS ops_startup_logs;

DROP TABLE IF EXISTS config_settings_history;

DROP TABLE IF EXISTS config_settings_values;

DROP TABLE IF EXISTS auth_sessions_tokens;

DROP TABLE IF EXISTS core_workstations_registry;

DROP TABLE IF EXISTS auth_roles_assignments;

DROP TABLE IF EXISTS core_users_profiles;

DROP TABLE IF EXISTS auth_roles_catalog;

SET FOREIGN_KEY_CHECKS = 1;