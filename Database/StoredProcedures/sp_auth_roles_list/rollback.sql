-- Rollback: sp_auth_roles_list
-- Engine: MySQL 5.7
-- Reversal of 006-user-management-and-permissions (task T028)
--
-- Drops the read. It owns no data: the rows it returns live in auth_roles_catalog, which predates this feature.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_roles_list;
