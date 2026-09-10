-- ============================================================
-- Object:      mtm_mock database bootstrap
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Bootstrap / rollback
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T007)
-- ============================================================
--
-- WARNING — DESTRUCTIVE.
-- Drops the entire `mtm_mock` mirror cache, including every cached Infor Visual shape.
-- This is safe by design: the cache is never authoritative for internal application data
-- (FR-027) and is rebuilt by the refresh procedures. It does NOT touch `mtm_waitlist`,
-- `mtm_wip_application_winforms`, or `mtm_receiving_application`.
-- ============================================================

DROP DATABASE IF EXISTS mtm_mock;
