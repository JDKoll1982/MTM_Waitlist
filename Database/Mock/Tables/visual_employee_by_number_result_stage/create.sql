-- ============================================================
-- Object:      visual_employee_by_number_result_stage
-- Database:    mtm_mock
-- Kind:        Table / create (staging twin, shape 6 of 6)
-- Feature:     011-identifier-first-signin (task T005)
-- ============================================================
--
-- Identical-structure staging twin used by sp_visual_employee_by_number_refresh for the atomic
-- live-table swap. Structure copied from visual_employee_by_number_result.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_employee_by_number_result_stage (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    employee_number VARCHAR(32) NOT NULL,
    first_name VARCHAR(128) NULL,
    last_name VARCHAR(128) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_employee_by_number_result_stage_employee_number (employee_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
