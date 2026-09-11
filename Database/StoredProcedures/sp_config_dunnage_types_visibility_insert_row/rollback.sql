-- Rollback: sp_config_dunnage_types_visibility_insert_row
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
-- Drops the insert procedure added by create.sql. It writes rows only while it runs, so the rollback cannot lose
-- data; the rows it previously inserted are the operator's saved visibility choices and are left in place.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_dunnage_types_visibility_insert_row;
