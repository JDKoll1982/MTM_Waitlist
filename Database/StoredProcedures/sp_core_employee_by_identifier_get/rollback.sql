-- Rollback: sp_core_employee_by_identifier_get
-- Engine: MySQL 5.7
-- Feature: 004-unified-card-item-picker (task T143)
-- Drops the read procedure added by create.sql. It owns no data, so the rollback cannot lose a row.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_employee_by_identifier_get;
