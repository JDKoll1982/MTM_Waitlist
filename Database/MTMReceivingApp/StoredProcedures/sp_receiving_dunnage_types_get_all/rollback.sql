-- Rollback: sp_receiving_dunnage_types_get_all
-- Engine: MySQL 5.7
-- Feature: 001-module-mock-visual-fallback (task T095)
-- Drops the read procedure added by create.sql. It owns no data, so the rollback cannot lose a row.

USE mtm_receiving_application;

DROP PROCEDURE IF EXISTS sp_receiving_dunnage_types_get_all;
