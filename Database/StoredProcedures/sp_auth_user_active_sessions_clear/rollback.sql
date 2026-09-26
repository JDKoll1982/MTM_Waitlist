-- Rollback: sp_auth_user_active_sessions_clear
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T045)
-- Drops the clear procedure. It owns no data: rows it cleared stay cleared, and re-issuing them is a sign-in,
-- not a rollback.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_auth_user_active_sessions_clear;
