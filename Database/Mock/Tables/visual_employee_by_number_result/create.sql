-- ============================================================
-- Object:      visual_employee_by_number_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 6 of 6)
-- Feature:     011-identifier-first-signin (task T004)
-- Shape key:   employee_by_number
-- Source read: Database/InforVisual/Queues/Module_Startup/Queries/GetEmployeeByNumber.sql
-- Caller:      IdentifierSignInService (the self set-up path)
-- ============================================================
--
-- Stores the flattened result of the Infor Visual employee lookup so the set-up path can confirm a
-- person's name while Infor Visual is unreachable (FR-032). Read exactly once from a snapshot: the
-- live table always resolves to a complete result because refreshes swap it atomically with its
-- _stage twin.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_employee_by_number_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    employee_number VARCHAR(32) NOT NULL,
    first_name VARCHAR(128) NULL,
    last_name VARCHAR(128) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_employee_by_number_result_employee_number (employee_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
