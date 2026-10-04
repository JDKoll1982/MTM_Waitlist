-- Rollback: sp_core_computers_registry_display_name_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
-- Drops the reader. It owns no data: the registry row it reports stays where the registry's own artifacts put it.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_display_name_get;
