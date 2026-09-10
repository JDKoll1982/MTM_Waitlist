-- ============================================================
-- Object:      visual_work_order_lookup_result_stage
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (stage twin of shape 1)
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T009)
-- ============================================================
--
-- Identical-structure staging twin. Written ONLY by sp_visual_work_order_lookup_refresh and
-- never read by the application. The refresh procedure truncates it, loads the complete new
-- result, then performs an atomic multi-table RENAME TABLE swap with the live mirror.
--
-- The index name is intentionally identical to the live mirror's so the swap does not change
-- the live table's key names (the unique key travels with the table).
-- See data-model.md §3.6.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_work_order_lookup_result_stage (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    normalized_work_order VARCHAR(64) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    description VARCHAR(255) NULL,
    work_center VARCHAR(32) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_work_order_lookup_result_work_order (normalized_work_order, part_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
