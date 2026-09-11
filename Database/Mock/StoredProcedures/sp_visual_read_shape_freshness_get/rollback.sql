-- ============================================================
-- Stored Procedure: sp_visual_read_shape_freshness_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7
-- Kind:       StoredProcedure / rollback (drop)
-- Created:    2026-09-10
-- Feature:    001-module-mock-visual-fallback (task T124)
-- ============================================================
--
-- Matching rollback for sp_visual_read_shape_freshness_get/create.sql.
-- The procedure owns no persisted state: dropping it removes the freshness report and leaves every
-- mirror table, stage twin, and refresh/get procedure untouched.
--
-- Rollback order note: this procedure is read-only, so it can be dropped before or after the tables
-- it reports on. It must be dropped BEFORE the tables only if the deployment script replays
-- `create.sql` files in a single pass that also drops tables.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_read_shape_freshness_get;
