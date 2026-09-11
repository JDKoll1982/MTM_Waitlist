-- Rollback: sp_config_dunnage_types_visibility_delete_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
--
-- Drops the delete-all procedure added by create.sql. It clears rows only while it runs, so dropping it cannot
-- lose data — and it must never be *executed* as part of a rollback, because the rows it removes are the
-- operator's saved visibility choices, not something this artifact created.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_delete_all;
