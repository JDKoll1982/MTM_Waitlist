-- ============================================================
-- Validation: refresh_swap_smoke
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Feature:    001-module-mock-visual-fallback
-- Proves:     FR-006 / SC-005 — the refresh replaces the live mirror atomically and the
--             live table name always resolves to a COMPLETE snapshot.
-- Run:        mysql -h <host> -u <user> mtm_mock < this_file
-- ============================================================
--
-- This is a smoke/diagnostic script, not part of the deploy path (deploy_mtm_mock.ps1 does not
-- pick up Database/Mock/Validation/**). It writes and then removes its own rows, and it leaves
-- the live mirror holding a deterministic 1-row snapshot at the end.
--
-- Expected: the 'live' selects always return a complete set (never empty, never partial), and no
-- table whose name ends in `_prev` persists between cycles.

USE mtm_mock;

-- ------------------------------------------------------------
-- 0. Cold-cache starting point (FR-017): a seeded row, is_seed_content = 1
-- ------------------------------------------------------------
INSERT INTO visual_work_order_lookup_result
    (normalized_work_order, part_number, description, work_center, refreshed_utc, is_seed_content)
VALUES
    ('ZZ-SEED-VALIDATION', 'ZZ-SEED-PART', 'Seeded baseline row', 'ZZ-SEED-WC', UTC_TIMESTAMP(), 1);

SELECT '0. BEFORE refresh: live holds seed content' AS step;
SELECT normalized_work_order, part_number, is_seed_content
FROM visual_work_order_lookup_result
ORDER BY part_number;

-- ------------------------------------------------------------
-- 1. Refresh with a 2-row payload
-- ------------------------------------------------------------
CALL sp_visual_work_order_lookup_refresh(
    '[{"NormalizedWorkOrder":"ZZ-WO-1","PartNumber":"ZZ-P1","Description":"First","WorkCenter":"ZZ-WC1"},
      {"NormalizedWorkOrder":"ZZ-WO-2","PartNumber":"ZZ-P2","Description":"Second","WorkCenter":"ZZ-WC2"}]');

SELECT '1a. AFTER refresh: live holds exactly the 2 new rows, is_seed_content = 0' AS step;
SELECT normalized_work_order, part_number, is_seed_content
FROM visual_work_order_lookup_result
ORDER BY part_number;

SELECT '1b. stage holds the PREVIOUS (seed) snapshot' AS step;
SELECT normalized_work_order, part_number, is_seed_content
FROM visual_work_order_lookup_result_stage
ORDER BY part_number;

SELECT '1c. no transient _prev table persists' AS step;
SELECT COUNT(*) AS prev_table_count
FROM information_schema.tables
WHERE table_schema = 'mtm_mock' AND table_name LIKE '%\_prev';

-- ------------------------------------------------------------
-- 2. Second refresh proves the three-name swap is repeatable
-- ------------------------------------------------------------
CALL sp_visual_work_order_lookup_refresh(
    '[{"NormalizedWorkOrder":"ZZ-WO-3","PartNumber":"ZZ-P3","Description":"Third","WorkCenter":"ZZ-WC3"}]');

SELECT '2a. AFTER refresh #2: live holds exactly 1 row' AS step;
SELECT normalized_work_order, part_number, is_seed_content
FROM visual_work_order_lookup_result;

SELECT '2b. stage holds the refresh #1 snapshot (2 rows)' AS step;
SELECT normalized_work_order, part_number
FROM visual_work_order_lookup_result_stage
ORDER BY part_number;

-- ------------------------------------------------------------
-- 3. An empty payload is a legitimate snapshot (FR-024), not a failure
-- ------------------------------------------------------------
CALL sp_visual_work_order_lookup_refresh('[]');

SELECT '3. empty payload produced a complete (empty) live snapshot' AS step;
SELECT COUNT(*) AS live_row_count FROM visual_work_order_lookup_result;

-- ------------------------------------------------------------
-- 4. Clean up: leave a deterministic 1-row live snapshot, no seed rows
-- ------------------------------------------------------------
CALL sp_visual_work_order_lookup_refresh(
    '[{"NormalizedWorkOrder":"ZZ-VALIDATION","PartNumber":"ZZ-VALIDATION-PART","Description":"Validation sentinel","WorkCenter":"ZZ-WC"}]');

TRUNCATE TABLE visual_work_order_lookup_result_stage;

SELECT '4. cleaned up' AS step;
SELECT normalized_work_order, part_number, is_seed_content
FROM visual_work_order_lookup_result;
