-- Rollback: sp_core_computers_registry_lookup_by_name_get
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Drops the read procedure added by create.sql. It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_lookup_by_name_get;
