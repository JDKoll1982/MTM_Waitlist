-- Rollback: sp_auth_remembered_sign_ins_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
-- Drops the read procedure. It owns no data and changes nothing it reads.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_remembered_sign_ins_get;
