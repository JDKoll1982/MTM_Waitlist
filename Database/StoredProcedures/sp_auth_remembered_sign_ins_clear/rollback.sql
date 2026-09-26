-- Rollback: sp_auth_remembered_sign_ins_clear
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
-- Drops the clear procedure. It owns no data: a cleared payload is not recoverable by re-creating a procedure,
-- and that is the point of clearing it.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_remembered_sign_ins_clear;
