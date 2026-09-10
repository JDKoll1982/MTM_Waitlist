-- ============================================================
-- Object:      visual_work_order_lookup_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 1 of 5)
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T009)
-- Shape key:   work_order_lookup
-- Source read: Database/InforVisual/Queues/Module_Setup/Queries/LookupWorkOrder.sql
-- Caller:      SetupLookupService.LookupWorkOrderFromBackendAsync
-- ============================================================
--
-- Stores the flattened result of the Infor Visual work-order lookup read so the app can
-- serve a complete, identically shaped result while Infor Visual is unreachable
-- (FR-002, FR-004). Read exactly once from a snapshot: the live table always resolves to a
-- complete result because refreshes swap it atomically with its _stage twin (FR-006).
--
-- DDL-REVIEW DEVIATION (R9) — approved by the Tech Lead (database reviewer) at the
-- Phase 1 DDL review:
--   The locked DB ruleset (Database/Database-Ruleset.md) lists `work_order` as a banned
--   word. The shape key and the mirrored business entity are `work_order` by deliberate
--   decision (matching the frozen shape catalog in data-model.md §3.1 and the existing
--   `22_mock_work_orders` precedent). The deviation is surfaced here rather than silently
--   diverging. Cross-reference: research.md R9, data-model.md §3.1.
--
-- Banned-word note also applies to `visual_disposition_input_result.work_order` (shape 5).
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_work_order_lookup_result (
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
