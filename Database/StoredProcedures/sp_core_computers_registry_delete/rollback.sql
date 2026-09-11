-- Rollback: sp_core_computers_registry_delete
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T092)
-- Drops the delete procedure added by create.sql. It deletes rows only while it runs, so the rollback cannot
-- lose data; the caller's former inline DELETE is not restored (see the upsert rollback for why).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computers_registry_delete;
