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
MODIFY COLUMN work_order_status VARCHAR(8) NULL COMMENT 'Raw Visual WORK_ORDER.STATUS code; meaning owned by RequestDispositionStatusCodes.',
MODIFY COLUMN open_work_order_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Open quantity still on the order.',
MODIFY COLUMN finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Finished-goods on-hand quantity for the part.',
MODIFY COLUMN has_outside_vendor_operation TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 when an operation carries vendor/service fields.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';

ALTER TABLE visual_disposition_input_result_stage COMMENT = 'Staging twin used by sp_visual_disposition_input_refresh for the atomic live-table swap.';

ALTER TABLE visual_disposition_input_result_stage
MODIFY COLUMN id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key.',
MODIFY COLUMN work_order VARCHAR(64) NOT NULL COMMENT 'Normalized work order used as the lookup predicate.',
MODIFY COLUMN part_number VARCHAR(64) NOT NULL COMMENT 'Finished-product part on the work order.',
MODIFY COLUMN work_order_status VARCHAR(8) NULL COMMENT 'Raw Visual WORK_ORDER.STATUS code; meaning owned by RequestDispositionStatusCodes.',
MODIFY COLUMN open_work_order_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Open quantity still on the order.',
MODIFY COLUMN finished_goods_quantity DECIMAL(18,4) NOT NULL DEFAULT 0 COMMENT 'Finished-goods on-hand quantity for the part.',
MODIFY COLUMN has_outside_vendor_operation TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 when an operation carries vendor/service fields.',
MODIFY COLUMN refreshed_utc DATETIME NOT NULL COMMENT 'UTC time of the refresh that produced this row.',
MODIFY COLUMN is_seed_content TINYINT(1) NOT NULL DEFAULT 0 COMMENT '1 for baseline seed rows; 0 for refreshed content.';
