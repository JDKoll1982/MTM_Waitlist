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
