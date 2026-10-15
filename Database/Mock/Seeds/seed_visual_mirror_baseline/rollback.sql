-- ============================================================
-- Rollback Seed: seed_visual_mirror_baseline
-- Database: mtm_mock
-- Feature:  001-module-mock-visual-fallback (task T026)
-- ============================================================
--
-- Removes ONLY seed content. Real refreshed rows (is_seed_content = 0) are deliberately kept:
-- this rollback must never discard a working cache.
-- ============================================================

USE mtm_mock;

DELETE FROM visual_work_order_lookup_result     WHERE is_seed_content = 1;
DELETE FROM visual_operation_sequences_result   WHERE is_seed_content = 1;
DELETE FROM visual_subordinate_parts_result     WHERE is_seed_content = 1;
DELETE FROM visual_inventory_locations_result   WHERE is_seed_content = 1;
DELETE FROM visual_disposition_input_result     WHERE is_seed_content = 1;
-- `employee_by_number` (shape 6, feature 011) and `part_warehouse_quantities` (shape 7, feature 012) arrived
-- after this rollback was written, so neither was listed. The gap is closed here: without these two lines a
-- re-install can neither seed nor clear those shapes' baseline rows.
DELETE FROM visual_employee_by_number_result    WHERE is_seed_content = 1;
DELETE FROM visual_part_warehouse_quantities_result WHERE is_seed_content = 1;
