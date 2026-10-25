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
--
-- `work_center` is part of the key because the routing answer is anchored on it: a work order's
-- "next operation" differs per work centre, so one row per (work order, part) could only ever be
-- right for one of them. `'*'` is the wildcard the baseline seed uses for rows captured before the
-- column existed; `sp_visual_disposition_input_get` prefers an exact match and only then the
-- wildcard, and a wildcard row carries NULL in both routing columns, so it never claims a routing
-- answer it does not have.
--
-- `next_operation_resource_id` / `has_remaining_operation` are the routing, and both are NULL when
-- it was not read: NULL is "unknown", 0/other-resource is "no operation remains", and a resource id
-- is the operation that follows the job's own (research 2026-10-23).
--
-- `has_customer_demand` decides Finished Goods against WIP once nothing remains in the routing: a part
-- with customer-order lines is the end product, and a part nothing buys exists only to feed another
-- work order. NULL when the read did not carry it.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_disposition_input_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    work_order VARCHAR(64) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    work_center VARCHAR(64) NOT NULL DEFAULT '*',
    work_order_status VARCHAR(8) NULL,
    open_work_order_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    has_outside_vendor_operation TINYINT(1) NOT NULL DEFAULT 0,
    next_operation_resource_id VARCHAR(16) NULL,
    has_remaining_operation TINYINT(1) NULL,
    has_customer_demand TINYINT(1) NULL,
    product_code VARCHAR(64) NULL,
    commodity_code VARCHAR(64) NULL,
    part_description VARCHAR(256) NULL,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_disposition_input_result_lookup_key (work_order, part_number, work_center)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
