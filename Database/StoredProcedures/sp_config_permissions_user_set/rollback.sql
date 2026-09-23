-- Rollback: sp_config_permissions_user_set
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T054)
--
-- Drops the change-set write. It owns no data of its own: the rows it writes live in config_settings_values and
-- config_settings_history, which both predate this feature.
--
-- Dropping rather than leaving it is the honest reversal. Before this feature there was no per-person permission
-- write at all, so a leftover procedure would be a write path with no caller, no owner and no reader, and the
-- first thing it would do is write a per-person row under a scope order nobody is checking.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_permissions_user_set;
