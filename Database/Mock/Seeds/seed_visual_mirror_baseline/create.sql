-- ============================================================
-- Seed: seed_visual_mirror_baseline
-- Database: mtm_mock
-- Engine:   MySQL 5.7 (verified on 9.6)
-- Kind:     Seed / create
-- Created:  2026-09-09
-- Feature:  001-module-mock-visual-fallback (task T026, FR-017)
-- ============================================================
--
-- Purpose
-- -------
-- A fresh `mtm_mock` has never been refreshed. Without baseline rows the very first fallback
-- read (before the service's first scheduled refresh) would return an empty set for every
-- journey. These rows make a fresh install serve a usable, coherent result instead (FR-017).
--
-- Every row here carries `is_seed_content = 1`. That flag is what lets the read-status surface
-- report `IsSeedContentOnly = true` and show "seeded content, not yet refreshed" rather than
-- reporting a fabricated refresh age (FR-022, data-model.md §3).
--
-- The seed rows form ONE coherent journey so every shape is demonstrable end-to-end:
--   work order SEED-WO-100 / part SEED-PART-100
--     -> 2 operation sequences (10, 20)
--     -> 2 subordinate parts on sequence 10
--     -> 2 inventory locations
--     -> 1 disposition snapshot
--
-- Re-runnable
-- -----------
-- `INSERT IGNORE` respects each mirror's unique key, so re-running this seed neither duplicates
-- rows nor overwrites real refreshed content for the same key. Refreshes always write
-- `is_seed_content = 0` and wholesale-replace the live table, so seed rows disappear on the first
-- successful refresh.
-- ============================================================

USE mtm_mock;

SET NAMES utf8mb4;

-- Shape 1: work_order_lookup
INSERT IGNORE INTO visual_work_order_lookup_result
    (normalized_work_order, part_number, description, work_center, refreshed_utc, is_seed_content)
VALUES
    ('SEED-WO-100', 'SEED-PART-100', 'Seeded baseline part', 'SEED-WC-A', UTC_TIMESTAMP(), 1);

-- Shape 2: operation_sequences
INSERT IGNORE INTO visual_operation_sequences_result
    (normalized_work_order, part_number, sequence_number, description, refreshed_utc, is_seed_content)
VALUES
    ('SEED-WO-100', 'SEED-PART-100', 10, 'Operation 10 / SEED-WC-A', UTC_TIMESTAMP(), 1),
    ('SEED-WO-100', 'SEED-PART-100', 20, 'Operation 20 / Unassigned', UTC_TIMESTAMP(), 1);

-- Shape 3: subordinate_parts
INSERT IGNORE INTO visual_subordinate_parts_result
    (normalized_work_order, parent_part_number, sequence_number, category, part_number,
     description, location, user8, on_hand_quantity, refreshed_utc, is_seed_content)
VALUES
    ('SEED-WO-100', 'SEED-PART-100', 10, 'Coil', 'SEED-MMC-1', 'Seeded coil', 'SEED-A0-01', '', 1234.5000, UTC_TIMESTAMP(), 1),
    ('SEED-WO-100', 'SEED-PART-100', 10, 'Component', 'SEED-CMP-1', 'Seeded component', 'SEED-B0-02', 'SEED-U8', 12.0000, UTC_TIMESTAMP(), 1);

-- Shape 4: inventory_locations
INSERT IGNORE INTO visual_inventory_locations_result
    (part_number, location, on_hand_quantity, refreshed_utc, is_seed_content)
VALUES
    ('SEED-PART-100', 'SEED-A0-01', 500.2500, UTC_TIMESTAMP(), 1),
    ('SEED-PART-100', 'SEED-A0-02', 0.7500, UTC_TIMESTAMP(), 1);

-- Shape 5: disposition_input
INSERT IGNORE INTO visual_disposition_input_result
    (work_order, part_number, work_order_status, open_work_order_quantity,
     finished_goods_quantity, has_outside_vendor_operation, refreshed_utc, is_seed_content)
VALUES
    ('SEED-WO-100', 'SEED-PART-100', 'R', 40.0000, 12.5000, 0, UTC_TIMESTAMP(), 1);
