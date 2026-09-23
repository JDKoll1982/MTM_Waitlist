-- Create MTM_Waitlist application database
-- Engine: MySQL 5.7
-- Run once per target environment before executing any other migrations.
--
-- This file is the FIRST step of the fresh-store path, and only that: it drops and recreates the database, so
-- it can never be part of the path that updates a store which already exists. The fresh store's shape comes
-- from the artifacts that follow it, in this order:
--   Database/Tables/**/create.sql      the tables and their columns (rolled up in Tables/AllTables.sql)
--   Database/Functions/**/create.sql   the settings scope rank function
--   Database/StoredProcedures/**       every procedure, including feature 006's user-management reads
--   Database/Seeds/**/create.sql       the baseline rows
--   Database/Bootstrap/update_table_descriptions.sql
--                                      table and column descriptions, plus the guarded column additions and the
--                                      guarded new-table creation that a store which ALREADY exists needs. It runs
--                                      last, so the four feature-006 columns and auth_user_management_audit reach
--                                      a fresh store and an existing store through the same blocks.
--                                      Feature 008's config_images_locations_history and its widened
--                                      config_images_locations.scope comment reach both stores the same way: this
--                                      file creates the database and nothing else, and the guarded block in that
--                                      maintenance file is what gives an existing store the new table.

DROP DATABASE IF EXISTS mtm_waitlist;

CREATE DATABASE mtm_waitlist CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE mtm_waitlist;