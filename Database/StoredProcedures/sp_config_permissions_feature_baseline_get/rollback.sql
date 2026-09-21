-- Rollback: sp_config_permissions_feature_baseline_get
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T060)
--
-- Drops the baseline read. It owns no data: it reports rows that live in config_settings_values, which predates
-- this feature, and it writes nothing.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_feature_baseline_get;
