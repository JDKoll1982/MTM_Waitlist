-- Rollback: sp_auth_user_roles_get
-- Engine: MySQL 5.7
-- Reversal of 010-startup-rebuild (task T195)
--
-- Drops the read. It owns no data: the rows it returns live in auth_roles_assignments joined to
-- auth_roles_catalog, both of which predate this feature. Reverting the application side of T195 removes the only
-- caller, so an environment promoted from an older build has nothing left that names this procedure.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_roles_get;
