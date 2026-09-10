-- ============================================================
-- Object:      visual_disposition_input_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 5 of 5)
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T013)
-- Shape key:   disposition_input
-- Source read: Database/InforVisual/Queues/Module_Waitlist/Queries/GetDispositionInput.sql
-- Caller:      RequestDispositionResolver.GetDispositionInputAsync
-- ============================================================
--
-- See visual_work_order_lookup_result/create.sql for the shared mirror-table rationale and
-- the approved R9 `work_order` naming deviation (this table carries the same banned word).
--
-- `work_order_status` remains a raw Visual status code (C/R/U/F/X). The status MEANING is
-- owned solely by RequestDispositionStatusCodes (Open = {R,U,F}, Closed = {C}, X never FG) —
-- this cache never interprets it (data-model.md §3.5, FR-018).
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_disposition_input_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    work_order VARCHAR(64) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    work_order_status VARCHAR(8) NULL,
    open_work_order_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    has_outside_vendor_operation TINYINT(1) NOT NULL DEFAULT 0,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_disposition_input_result_lookup_key (work_order, part_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
