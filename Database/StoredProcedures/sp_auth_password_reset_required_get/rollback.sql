-- Rollback: sp_auth_password_reset_required_get
-- Engine: MySQL 5.7
-- Drops the read procedure added by create.sql. It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_password_reset_required_get;
