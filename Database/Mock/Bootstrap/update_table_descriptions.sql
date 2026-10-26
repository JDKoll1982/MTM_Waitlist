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
--   2026-10-26  shape 5 routing rework: the guarded blocks below put the seven columns and the
--               work-centre lookup key onto a mirror that was deployed before they existed
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

-- ============================================================
-- Existing mirrors are brought up to shape before they are described
-- ============================================================
-- `CREATE TABLE IF NOT EXISTS` cannot add a column to a table that already exists, and a plain
-- MODIFY against a column the store does not have stops this script on the first run after the
-- change. An `mtm_mock` deployed before the shape-5 routing rework therefore failed here with
-- "Unknown column 'work_center'", which aborted the whole cache phase of the install. So every
-- column shape 5 gained is added by a guarded block below, and the MODIFY statements that
-- describe them run afterwards.
--
-- The lookup key is replaced by the same guard rather than left alone: the read and the refresh
-- both address a row by (work order, part, work centre), and the older key without the work
-- centre could only ever hold one work centre's routing answer for a work order.
--
--   Owning artifacts: Database/Mock/Tables/visual_disposition_input_result/create.sql and
--     Database/Mock/Tables/visual_disposition_input_result_stage/create.sql.
--   A row that predates the change reads `'*'` in `work_center` — the wildcard the get procedure
--     already prefers last — and NULL in the routing columns, which the caller reads as "routing
--     not read" rather than "no operation remains".
-- ============================================================

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'work_center');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN work_center VARCHAR(64) NOT NULL DEFAULT ''*'' COMMENT ''Work centre the routing answer is anchored on; * marks a row seeded before the routing was read.'' AFTER part_number', 'SELECT ''visual_disposition_input_result.work_center already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'next_operation_resource_id');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN next_operation_resource_id VARCHAR(16) NULL COMMENT ''RESOURCE_ID of the first open operation after the job own; OUTSIDE_SERVICE means Outside Service.'' AFTER has_outside_vendor_operation', 'SELECT ''visual_disposition_input_result.next_operation_resource_id already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'has_remaining_operation');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN has_remaining_operation TINYINT(1) NULL COMMENT ''1 when an open operation remains after the job own, 0 when nothing follows it, NULL when the routing was not read.'' AFTER next_operation_resource_id', 'SELECT ''visual_disposition_input_result.has_remaining_operation already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'has_customer_demand');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN has_customer_demand TINYINT(1) NULL COMMENT ''1 when the part has customer-order lines in Visual, so it is a part the plant sells; NULL when not read. Decides Finished Goods against WIP when nothing follows.'' AFTER has_remaining_operation', 'SELECT ''visual_disposition_input_result.has_customer_demand already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'product_code');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN product_code VARCHAR(64) NULL COMMENT ''Infor Visual PART.PRODUCT_CODE: the plant part category (FG_PRESS, RM_PRESS, MFG_TOOLING, EXP_*).'' AFTER has_customer_demand', 'SELECT ''visual_disposition_input_result.product_code already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'commodity_code');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN commodity_code VARCHAR(64) NULL COMMENT ''Infor Visual PART.COMMODITY_CODE: the material or part kind (E Stamped Item, M Matl Coil, M Fast Fastener...).'' AFTER product_code', 'SELECT ''visual_disposition_input_result.commodity_code already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND column_name = 'part_description');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result ADD COLUMN part_description VARCHAR(256) NULL COMMENT ''Infor Visual PART.DESCRIPTION, whose words break a customer-supplied tie.'' AFTER commodity_code', 'SELECT ''visual_disposition_input_result.part_description already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @lookup_key_columns := (SELECT COALESCE(GROUP_CONCAT(column_name ORDER BY seq_in_index), '') FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND index_name = 'uq_visual_disposition_input_result_lookup_key');
SET @sql_stmt := IF(@lookup_key_columns NOT IN ('', 'work_order,part_number,work_center'), 'ALTER TABLE visual_disposition_input_result DROP INDEX uq_visual_disposition_input_result_lookup_key', 'SELECT ''visual_disposition_input_result lookup key already includes the work centre''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_lookup_key := (SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result' AND index_name = 'uq_visual_disposition_input_result_lookup_key');
SET @sql_stmt := IF(@has_lookup_key = 0, 'ALTER TABLE visual_disposition_input_result ADD UNIQUE KEY uq_visual_disposition_input_result_lookup_key (work_order, part_number, work_center)', 'SELECT ''visual_disposition_input_result lookup key is present''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'work_center');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN work_center VARCHAR(64) NOT NULL DEFAULT ''*'' COMMENT ''Work centre the routing answer is anchored on; * marks a row seeded before the routing was read.'' AFTER part_number', 'SELECT ''visual_disposition_input_result_stage.work_center already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'next_operation_resource_id');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN next_operation_resource_id VARCHAR(16) NULL COMMENT ''RESOURCE_ID of the first open operation after the job own; OUTSIDE_SERVICE means Outside Service.'' AFTER has_outside_vendor_operation', 'SELECT ''visual_disposition_input_result_stage.next_operation_resource_id already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'has_remaining_operation');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN has_remaining_operation TINYINT(1) NULL COMMENT ''1 when an open operation remains after the job own, 0 when nothing follows it, NULL when the routing was not read.'' AFTER next_operation_resource_id', 'SELECT ''visual_disposition_input_result_stage.has_remaining_operation already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'has_customer_demand');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN has_customer_demand TINYINT(1) NULL COMMENT ''1 when the part has customer-order lines in Visual, so it is a part the plant sells; NULL when not read. Decides Finished Goods against WIP when nothing follows.'' AFTER has_remaining_operation', 'SELECT ''visual_disposition_input_result_stage.has_customer_demand already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'product_code');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN product_code VARCHAR(64) NULL COMMENT ''Infor Visual PART.PRODUCT_CODE: the plant part category (FG_PRESS, RM_PRESS, MFG_TOOLING, EXP_*).'' AFTER has_customer_demand', 'SELECT ''visual_disposition_input_result_stage.product_code already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'commodity_code');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN commodity_code VARCHAR(64) NULL COMMENT ''Infor Visual PART.COMMODITY_CODE: the material or part kind (E Stamped Item, M Matl Coil, M Fast Fastener...).'' AFTER product_code', 'SELECT ''visual_disposition_input_result_stage.commodity_code already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_column := (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND column_name = 'part_description');
SET @sql_stmt := IF(@has_column = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD COLUMN part_description VARCHAR(256) NULL COMMENT ''Infor Visual PART.DESCRIPTION, whose words break a customer-supplied tie.'' AFTER commodity_code', 'SELECT ''visual_disposition_input_result_stage.part_description already exists''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @lookup_key_columns := (SELECT COALESCE(GROUP_CONCAT(column_name ORDER BY seq_in_index), '') FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND index_name = 'uq_visual_disposition_input_result_lookup_key');
SET @sql_stmt := IF(@lookup_key_columns NOT IN ('', 'work_order,part_number,work_center'), 'ALTER TABLE visual_disposition_input_result_stage DROP INDEX uq_visual_disposition_input_result_lookup_key', 'SELECT ''visual_disposition_input_result_stage lookup key already includes the work centre''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @has_lookup_key := (SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'visual_disposition_input_result_stage' AND index_name = 'uq_visual_disposition_input_result_lookup_key');
SET @sql_stmt := IF(@has_lookup_key = 0, 'ALTER TABLE visual_disposition_input_result_stage ADD UNIQUE KEY uq_visual_disposition_input_result_lookup_key (work_order, part_number, work_center)', 'SELECT ''visual_disposition_input_result_stage lookup key is present''');
PREPARE stmt FROM @sql_stmt; EXECUTE stmt; DEALLOCATE PREPARE stmt;

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
MODIFY COLUMN product_code VARCHAR(64) NULL COMMENT 'Infor Visual PART.PRODUCT_CODE: the plant part category (FG_PRESS, RM_PRESS, MFG_TOOLING, EXP_*).',
MODIFY COLUMN commodity_code VARCHAR(64) NULL COMMENT 'Infor Visual PART.COMMODITY_CODE: the material or part kind (E Stamped Item, M Matl Coil, M Fast Fastener...).',
MODIFY COLUMN part_description VARCHAR(256) NULL COMMENT 'Infor Visual PART.DESCRIPTION, whose words break a customer-supplied tie.',
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
MODIFY COLUMN product_code VARCHAR(64) NULL COMMENT 'Infor Visual PART.PRODUCT_CODE: the plant part category (FG_PRESS, RM_PRESS, MFG_TOOLING, EXP_*).',
MODIFY COLUMN commodity_code VARCHAR(64) NULL COMMENT 'Infor Visual PART.COMMODITY_CODE: the material or part kind (E Stamped Item, M Matl Coil, M Fast Fastener...).',
MODIFY COLUMN part_description VARCHAR(256) NULL COMMENT 'Infor Visual PART.DESCRIPTION, whose words break a customer-supplied tie.',
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
