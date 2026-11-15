-- ============================================================
-- Object:      visual_die_search_result_stage
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (stage twin of shape 12)
-- Created:     2026-11-15
-- Feature:     001-module-mock-visual-fallback (read shape 12, playbook step 2)
-- ============================================================
--
-- Identical-structure staging twin. Written ONLY by sp_visual_die_search_refresh and never read by the
-- application. The refresh procedure truncates it, loads the complete new result, then performs an atomic
-- multi-table RENAME TABLE swap with the live mirror.
--
-- The index names are intentionally identical to the live mirror's so the swap does not change the
-- live table's key names (the keys travel with the table). See data-model.md §3.6.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_die_search_result_stage (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    term VARCHAR(190) NOT NULL,
    die_number VARCHAR(64) NOT NULL,
    description VARCHAR(255) NULL,
    association VARCHAR(16) NOT NULL,
    location VARCHAR(64) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_die_search_result_key (term, die_number, association),
    KEY idx_visual_die_search_result_die_number (die_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;