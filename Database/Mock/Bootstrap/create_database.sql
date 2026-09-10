-- ============================================================
-- Object:      mtm_mock database bootstrap
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Bootstrap / create
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T007, FR-027)
-- ============================================================
--
-- Purpose
-- -------
-- Create the dedicated Infor Visual mirror-cache database. This database holds ONLY
-- cached copies of external Infor Visual read shapes. It is NEVER authoritative for
-- internal application data (FR-027) and has NO dependency on `mtm_waitlist`: it can be
-- created, dropped, and rebuilt without touching any internal store.
--
-- Re-runnable
-- -----------
-- Safe to run repeatedly. Creates the database only when absent; never drops or alters
-- an existing database.
--
-- Deploy order
-- ------------
--   1. Bootstrap/create_database.sql                 (this file)
--   2. Tables/<table>/create.sql   x 10              (5 mirrors + 5 stage twins)
--   3. StoredProcedures/<proc>/create.sql            (sp_visual_*)
--   4. Bootstrap/update_table_descriptions.sql       (mandatory maintenance file)
--   5. Seeds/seed_visual_mirror_baseline/create.sql  (FR-017 cold-cache content)
-- ============================================================

CREATE DATABASE IF NOT EXISTS mtm_mock
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_unicode_ci;

USE mtm_mock;
