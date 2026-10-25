-- ============================================================
-- Object:      visual_work_order_search_result_stage
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (stage twin of shape 8)
-- Created:     2026-10-21
-- Feature:     001-module-mock-visual-fallback (read shape 8, playbook step 2)
-- ============================================================
--
-- Identical-structure staging twin. Written ONLY by sp_visual_work_order_search_refresh and
-- never read by the application. The refresh procedure truncates it, loads the complete new
-- result, then performs an atomic multi-table RENAME TABLE swap with the live mirror.
--
-- The index names are intentionally identical to the live mirror's so the swap does not change the
-- live table's key names (the keys travel with the table).
-- See data-model.md §3.6.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_work_order_search_result_stage (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    work_order_text VARCHAR(32) NOT NULL,
    part_filter VARCHAR(64) NOT NULL,
    customer_filter VARCHAR(120) NOT NULL,
    work_order VARCHAR(32) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    part_description VARCHAR(255) NULL,
    customer_name VARCHAR(120) NULL,
    work_center VARCHAR(32) NULL,
    status VARCHAR(4) NULL,
    want_date DATE NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_work_order_search_result_work_order (work_order),
    KEY idx_visual_work_order_search_result_part_number (part_number),
    KEY idx_visual_work_order_search_result_customer_name (customer_name)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
