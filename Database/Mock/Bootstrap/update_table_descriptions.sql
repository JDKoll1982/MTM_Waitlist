-- ============================================================
-- Object:      mtm_mock table/column descriptions
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Bootstrap / maintenance
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T008)
-- ============================================================
--
-- MANDATORY maintenance file. Every schema change to `mtm_mock` must update this file in
-- the same change (locked DB ruleset; .github/instructions/database-schema-rules.instructions.md).
--
-- Updated by:
--   T008  initial (empty — no tables yet)
--   T014  five mirror tables + five stage twins
--   011   sixth mirror table (employee_by_number) + its stage twin
--   012   seventh mirror table (part_warehouse_quantities) + its stage twin
--   013   eighth mirror table (work_order_search) + its stage twin
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- Shape 1: work_order_lookup
-- ------------------------------------------------------------

ALTER TABLE visual_work_order_lookup_result COMMENT = 'Cached Infor Visual work-order lookup result (shape 1 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_work_order_lookup_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN normalized_work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work-order input used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Visual part number returned for the work order.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Part description (empty string when Visual has none).',
MODIFY COLUMN work_center VARCHAR(32) NULL COMMENT 'Primary work center (first operation resource) for the order.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_work_order_lookup_result_stage COMMENT = 'Staging twin used by sp_visual_work_order_lookup_refresh for the atomic live-table swap.';

ALTER TABLE visual_work_order_lookup_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN normalized_work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work-order input used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Visual part number returned for the work order.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Part description (empty string when Visual has none).',
MODIFY COLUMN work_center VARCHAR(32) NULL COMMENT 'Primary work center (first operation resource) for the order.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 2: operation_sequences
-- ------------------------------------------------------------

ALTER TABLE visual_operation_sequences_result COMMENT = 'Cached Infor Visual operation-sequence result (shape 2 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_operation_sequences_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN normalized_work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work-order input used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number the sequences belong to.',
MODIFY COLUMN sequence_number INT UNSIGNED NOT NULL COMMENT 'Visual operation sequence number.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Operation description (sequence / assigned resource).',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_operation_sequences_result_stage COMMENT = 'Staging twin used by sp_visual_operation_sequences_refresh for the atomic live-table swap.';

ALTER TABLE visual_operation_sequences_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN normalized_work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work-order input used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number the sequences belong to.',
MODIFY COLUMN sequence_number INT UNSIGNED NOT NULL COMMENT 'Visual operation sequence number.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Operation description (sequence / assigned resource).',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 3: subordinate_parts
-- ------------------------------------------------------------

ALTER TABLE visual_subordinate_parts_result COMMENT = 'Cached Infor Visual subordinate-parts result (shape 3 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_subordinate_parts_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN normalized_work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work-order input used as the lookup predicate.',
MODIFY COLUMN parent_part_number VARCHAR(64) NOT NULL COMMENT 'Part context of the operation whose requirements were read.',
MODIFY COLUMN sequence_number INT UNSIGNED NOT NULL COMMENT 'Operation sequence number the requirements belong to.',
MODIFY COLUMN category VARCHAR(64) NULL COMMENT 'Derived requirement category (Coil, Die, Flatstock, Component).',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Subordinate part number returned by the read.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Subordinate part description.',
MODIFY COLUMN location VARCHAR(64) NULL COMMENT 'Resolved location for the subordinate part.',
MODIFY COLUMN user8 VARCHAR(64) NULL COMMENT 'Visual PART.USER_8 value passed through unchanged.',
MODIFY COLUMN on_hand_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Pooled on-hand quantity for the subordinate part.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_subordinate_parts_result_stage COMMENT = 'Staging twin used by sp_visual_subordinate_parts_refresh for the atomic live-table swap.';

ALTER TABLE visual_subordinate_parts_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN normalized_work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work-order input used as the lookup predicate.',
MODIFY COLUMN parent_part_number VARCHAR(64) NOT NULL COMMENT 'Part context of the operation whose requirements were read.',
MODIFY COLUMN sequence_number INT UNSIGNED NOT NULL COMMENT 'Operation sequence number the requirements belong to.',
MODIFY COLUMN category VARCHAR(64) NULL COMMENT 'Derived requirement category (Coil, Die, Flatstock, Component).',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Subordinate part number returned by the read.',
MODIFY COLUMN description VARCHAR(255) NULL COMMENT 'Subordinate part description.',
MODIFY COLUMN location VARCHAR(64) NULL COMMENT 'Resolved location for the subordinate part.',
MODIFY COLUMN user8 VARCHAR(64) NULL COMMENT 'Visual PART.USER_8 value passed through unchanged.',
MODIFY COLUMN on_hand_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Pooled on-hand quantity for the subordinate part.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 4: inventory_locations
-- ------------------------------------------------------------

ALTER TABLE visual_inventory_locations_result COMMENT = 'Cached Infor Visual inventory-locations result (shape 4 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_inventory_locations_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number; both the read input and the returned part.',
MODIFY COLUMN location VARCHAR(64) NOT NULL COMMENT 'Visual location/warehouse code (V-A0-01 style).',
MODIFY COLUMN on_hand_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Pooled on-hand quantity for the part at the location.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_inventory_locations_result_stage COMMENT = 'Staging twin used by sp_visual_inventory_locations_refresh for the atomic live-table swap.';

ALTER TABLE visual_inventory_locations_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number; both the read input and the returned part.',
MODIFY COLUMN location VARCHAR(64) NOT NULL COMMENT 'Visual location/warehouse code (V-A0-01 style).',
MODIFY COLUMN on_hand_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Pooled on-hand quantity for the part at the location.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 5: disposition_input
-- ------------------------------------------------------------

ALTER TABLE visual_disposition_input_result COMMENT = 'Cached Infor Visual disposition-input result (shape 5 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_disposition_input_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work order used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Finished-product part on the work order.',
MODIFY COLUMN work_center VARCHAR(64) NOT NULL DEFAULT '*' COMMENT 'Work centre the routing answer is anchored on; * marks a row seeded before the routing was read.',
MODIFY COLUMN work_order_status VARCHAR(8) NULL COMMENT 'Raw Visual WORK_ORDER.STATUS code; meaning owned by RequestDispositionStatusCodes.',
MODIFY COLUMN open_work_order_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Open quantity still on the order.',
MODIFY COLUMN finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Finished-goods on-hand quantity for the part.',
MODIFY COLUMN has_outside_vendor_operation TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 when an operation carries vendor/service fields; the routing decides when it was read.',
MODIFY COLUMN next_operation_resource_id VARCHAR(16) NULL COMMENT 'RESOURCE_ID of the first open operation after the job own; OUTSIDE_SERVICE means Outside Service.',
MODIFY COLUMN has_remaining_operation TINYINT(1) NULL COMMENT '1 when an open operation remains after the job own, 0 when nothing follows it, NULL when the routing was not read.',
MODIFY COLUMN has_customer_demand TINYINT(1) NULL COMMENT '1 when the part has customer-order lines in Visual, so it is a part the plant sells; NULL when not read. Decides Finished Goods against WIP when nothing follows.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_disposition_input_result_stage COMMENT = 'Staging twin used by sp_visual_disposition_input_refresh for the atomic live-table swap.';

ALTER TABLE visual_disposition_input_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work order used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Finished-product part on the work order.',
MODIFY COLUMN work_center VARCHAR(64) NOT NULL DEFAULT '*' COMMENT 'Work centre the routing answer is anchored on; * marks a row seeded before the routing was read.',
MODIFY COLUMN work_order_status VARCHAR(8) NULL COMMENT 'Raw Visual WORK_ORDER.STATUS code; meaning owned by RequestDispositionStatusCodes.',
MODIFY COLUMN open_work_order_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Open quantity still on the order.',
MODIFY COLUMN finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Finished-goods on-hand quantity for the part.',
MODIFY COLUMN has_outside_vendor_operation TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 when an operation carries vendor/service fields; the routing decides when it was read.',
MODIFY COLUMN next_operation_resource_id VARCHAR(16) NULL COMMENT 'RESOURCE_ID of the first open operation after the job own; OUTSIDE_SERVICE means Outside Service.',
MODIFY COLUMN has_remaining_operation TINYINT(1) NULL COMMENT '1 when an open operation remains after the job own, 0 when nothing follows it, NULL when the routing was not read.',
MODIFY COLUMN has_customer_demand TINYINT(1) NULL COMMENT '1 when the part has customer-order lines in Visual, so it is a part the plant sells; NULL when not read. Decides Finished Goods against WIP when nothing follows.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 6: employee_by_number
-- ------------------------------------------------------------

ALTER TABLE visual_employee_by_number_result COMMENT = 'Cached Infor Visual employee lookup result (shape 6 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_employee_by_number_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN employee_number VARCHAR(32) NOT NULL COMMENT 'Employee number lookup key, matched verbatim against EMPLOYEE.ID.',
MODIFY COLUMN first_name VARCHAR(128) NULL COMMENT 'Employee first name (empty string when Visual has none).',
MODIFY COLUMN last_name VARCHAR(128) NULL COMMENT 'Employee last name (empty string when Visual has none).',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_employee_by_number_result_stage COMMENT = 'Staging twin used by sp_visual_employee_by_number_refresh for the atomic live-table swap.';

ALTER TABLE visual_employee_by_number_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN employee_number VARCHAR(32) NOT NULL COMMENT 'Employee number lookup key, matched verbatim against EMPLOYEE.ID.',
MODIFY COLUMN first_name VARCHAR(128) NULL COMMENT 'Employee first name (empty string when Visual has none).',
MODIFY COLUMN last_name VARCHAR(128) NULL COMMENT 'Employee last name (empty string when Visual has none).',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 7: part_warehouse_quantities
-- ------------------------------------------------------------

ALTER TABLE visual_part_warehouse_quantities_result COMMENT = 'Cached Infor Visual part-warehouse-quantities result (shape 7 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_part_warehouse_quantities_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number; both the read input and the returned part.',
MODIFY COLUMN warehouse_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'The part-wide on-hand sum over every location, filtered by location class. The card''s `Quantity in house` and `Quantity` cells read it (FR-090).',
MODIFY COLUMN finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'The same sum restricted to finished-goods locations; what `Item Type` and `Total Qty In House` need (FR-090, D38).',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_part_warehouse_quantities_result_stage COMMENT = 'Staging twin used by sp_visual_part_warehouse_quantities_refresh for the atomic live-table swap.';

ALTER TABLE visual_part_warehouse_quantities_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Part number; both the read input and the returned part.',
MODIFY COLUMN warehouse_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'The part-wide on-hand sum over every location, filtered by location class. The card''s `Quantity in house` and `Quantity` cells read it (FR-090).',
MODIFY COLUMN finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'The same sum restricted to finished-goods locations; what `Item Type` and `Total Qty In House` need (FR-090, D38).',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

-- ------------------------------------------------------------
-- Shape 8: work_order_search
-- ------------------------------------------------------------

ALTER TABLE visual_work_order_search_result COMMENT = 'Cached Infor Visual work-order search result (shape 8 mirror); complete snapshot, replaced atomically.';

ALTER TABLE visual_work_order_search_result
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN work_order_text VARCHAR(32) NOT NULL COMMENT 'Declared input, holding the row''s own work order; the search filters on work_order.',
MODIFY COLUMN part_filter VARCHAR(64) NOT NULL COMMENT 'Declared input, holding the row''s own part number; the search filters on part_number.',
MODIFY COLUMN customer_filter VARCHAR(120) NOT NULL COMMENT 'Declared input, holding the row''s own customer name; the search filters on customer_name.',
MODIFY COLUMN work_order VARCHAR(32) NOT NULL COMMENT 'Canonical WO-###### base id of the open work order.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'The part the work order builds; a search input and a returned column.',
MODIFY COLUMN part_description VARCHAR(255) NULL COMMENT 'Part description (empty string when Visual has none).',
MODIFY COLUMN customer_name VARCHAR(120) NULL COMMENT 'The customer''s real name from CUST_ORDER_ALLOC.CUST_NAME, never the customer id; empty for a stock order.',
MODIFY COLUMN work_center VARCHAR(32) NULL COMMENT 'Primary work center (first operation resource) for the order.',
MODIFY COLUMN status VARCHAR(4) NULL COMMENT 'Visual work-order status code: R released, U unreleased, F firmed.',
MODIFY COLUMN want_date DATE NULL COMMENT 'The order''s desired want date, used as the secondary sort.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_work_order_search_result_stage COMMENT = 'Staging twin used by sp_visual_work_order_search_refresh for the atomic live-table swap.';

ALTER TABLE visual_work_order_search_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN work_order_text VARCHAR(32) NOT NULL COMMENT 'Declared input, holding the row''s own work order; the search filters on work_order.',
MODIFY COLUMN part_filter VARCHAR(64) NOT NULL COMMENT 'Declared input, holding the row''s own part number; the search filters on part_number.',
MODIFY COLUMN customer_filter VARCHAR(120) NOT NULL COMMENT 'Declared input, holding the row''s own customer name; the search filters on customer_name.',
MODIFY COLUMN work_order VARCHAR(32) NOT NULL COMMENT 'Canonical WO-###### base id of the open work order.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'The part the work order builds; a search input and a returned column.',
MODIFY COLUMN part_description VARCHAR(255) NULL COMMENT 'Part description (empty string when Visual has none).',
MODIFY COLUMN customer_name VARCHAR(120) NULL COMMENT 'The customer''s real name from CUST_ORDER_ALLOC.CUST_NAME, never the customer id; empty for a stock order.',
MODIFY COLUMN work_center VARCHAR(32) NULL COMMENT 'Primary work center (first operation resource) for the order.',
MODIFY COLUMN status VARCHAR(4) NULL COMMENT 'Visual work-order status code: R released, U unreleased, F firmed.',
MODIFY COLUMN want_date DATE NULL COMMENT 'The order''s desired want date, used as the secondary sort.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';
