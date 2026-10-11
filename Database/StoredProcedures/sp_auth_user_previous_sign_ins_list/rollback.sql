-- Rollback: sp_auth_user_previous_sign_ins_list
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T235)
-- Drops the sign-in stage's previous-people read. It owns no data and changes nothing it reads.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_previous_sign_ins_list;
