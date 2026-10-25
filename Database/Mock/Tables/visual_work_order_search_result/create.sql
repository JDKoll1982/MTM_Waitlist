-- ============================================================
-- Object:      visual_work_order_search_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 8 of 8)
-- Created:     2026-10-21
-- Feature:     001-module-mock-visual-fallback (read shape 8, playbook step 2)
-- Shape key:   work_order_search
-- Source read: Database/InforVisual/Queues/Module_Setup/Queries/GetWorkOrderSearch.sql
-- Caller:      SetupLookupService.SearchWorkOrdersAsync
-- ============================================================
--
-- Stores the flattened result of the Infor Visual work-order search so the dialog can answer while
-- Infor Visual is unreachable (FR-002, FR-004). Read exactly once from a snapshot: the live table
-- always resolves to a complete result because refreshes swap it atomically with its _stage twin
-- (FR-006).
--
-- One row per work order. The customer is the latest CUST_ORDER_ALLOC allocation that names the order
-- (see the source read's header); an order with no allocation keeps an empty customer and is what a
-- customer-filtered search legitimately excludes.
--
-- The three `work_order_text` / `part_filter` / `customer_filter` columns are the shape's declared
-- INPUTS, held beside the outputs because the refresh procedure reads every declared parameter and
-- column out of the payload. They carry the row's own work order, part and customer, exactly as
-- `inventory_locations` holds its PartNumber input beside its PartNumber output; the read procedure
-- filters on the output columns.
--
-- No baseline seed rows ship for this shape. Unlike a lookup, an empty search result is a valid
-- answer ("nothing matches yet"), so a fresh install serves an empty set until the on-host service's
-- first refresh lands. This mirrors the `employee_by_number` decision.
--
-- Banned-word note: `work_order` appears in the artifact name and columns for the same reason
-- `visual_work_order_lookup_result` carries it (R9 deviation, data-model.md §3.1).
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_work_order_search_result (
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
