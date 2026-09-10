-- ============================================================
-- Object:      visual_inventory_locations_result_stage
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (stage twin of shape 4)
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T012)
-- ============================================================
--
-- Identical-structure staging twin; see visual_work_order_lookup_result_stage/create.sql.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_inventory_locations_result_stage (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    part_number VARCHAR(64) NOT NULL,
    location VARCHAR(64) NOT NULL,
    on_hand_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_inventory_locations_result_location (part_number, location)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
