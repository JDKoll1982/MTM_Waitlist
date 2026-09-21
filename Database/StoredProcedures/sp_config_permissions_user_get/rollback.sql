-- Rollback: sp_config_permissions_user_get
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T014)
--
-- Drops the read. It owns no data: the rows it returns live in config_settings_values, which predates this
-- feature.
--
-- Dropping rather than leaving it is the honest reversal: before this feature there was no per-person
-- permission read at all, so a leftover procedure would be a read path with no caller and no owner.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_user_get;
