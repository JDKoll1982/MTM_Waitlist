-- Rollback: sp_auth_remembered_sign_ins_upsert
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T046)
-- Drops the write procedure. It owns no data: the rows it wrote stay in `auth_remembered_sign_ins`, and the
-- table's own rollback is what removes them.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_remembered_sign_ins_upsert;
