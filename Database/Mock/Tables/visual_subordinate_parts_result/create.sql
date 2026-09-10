-- ============================================================
-- Object:      visual_subordinate_parts_result
-- Database:    mtm_mock
-- Engine:      MySQL 5.7
-- Kind:        Table / create (mirror, shape 3 of 5)
-- Created:     2026-09-09
-- Feature:     001-module-mock-visual-fallback (task T011)
-- Shape key:   subordinate_parts
-- Source read: Database/InforVisual/Queues/Module_Setup/Queries/GetSubordinateParts.sql
-- Caller:      SetupLookupService.GetSubordinatePartsFromBackendAsync
-- ============================================================
--
-- See visual_work_order_lookup_result/create.sql for the shared mirror-table rationale and
-- the approved R9 `work_order` naming deviation.
--
-- COLUMN RENAME (data-model.md §3.3): the live read takes the OPERATION's part number as an
-- input AND returns each SUBORDINATE part's number as an output — both are `PartNumber` in
-- the source. The input key is persisted as `parent_part_number` so that `part_number` in a
-- returned row means exactly what it means in the live result. sp_visual_subordinate_parts_get
-- exposes the input as `p_part_number`, so callers see no difference (FR-004).
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS visual_subordinate_parts_result (
    id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    normalized_work_order VARCHAR(64) NOT NULL,
    parent_part_number VARCHAR(64) NOT NULL,
    sequence_number INT UNSIGNED NOT NULL,
    category VARCHAR(64) NULL,
    part_number VARCHAR(64) NOT NULL,
    description VARCHAR(255) NULL,
    location VARCHAR(64) NULL,
    user8 VARCHAR(64) NULL,
    on_hand_quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
    refreshed_utc DATETIME NOT NULL,
    is_seed_content TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (id),
    UNIQUE KEY uq_visual_subordinate_parts_result_lookup_key
        (normalized_work_order, parent_part_number, sequence_number, part_number)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
