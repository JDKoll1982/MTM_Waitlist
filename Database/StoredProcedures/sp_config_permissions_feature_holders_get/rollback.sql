-- Rollback: sp_config_permissions_feature_holders_get
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T054)
--
-- Drops the holders read. It owns no data: it reports rows that live in config_settings_values, which predates
-- this feature, and it writes nothing.
--
-- Dropping rather than leaving it is the honest reversal: before this feature there was no who-holds-this read,
-- so a leftover procedure would be a read path with no caller and no owner.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_feature_holders_get;
