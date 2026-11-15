-- ============================================================
-- Stored Procedure: sp_visual_part_matches_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (refresh, shape 9 of 12)
-- Created:    2026-11-15
-- Feature:    001-module-mock-visual-fallback (read shape 9, playbook step 3)
-- Shape key:  part_matches
-- ============================================================
--
-- Purpose
-- -------
-- Replace the live mirror `visual_part_matches_result` with a complete new result set, atomically.
-- Readers address only the live table name, so they always observe either the complete previous
-- snapshot or the complete new one — never a partial set (FR-006, SC-005).
--
-- Contract
-- --------
--   p_rows : JSON array of result objects. Keys are the shape's declared inputs followed by its output
--            columns: "Term", "PartNumber", "Description".
--            A NULL or empty array produces an empty snapshot, which is a legitimate result for a
--            search (an empty live read is a valid answer — FR-024).
--
-- Steps
-- -----
--   1. TRUNCATE the stage twin.
--   2. Expand p_rows into the stage twin (refreshed_utc = UTC_TIMESTAMP(), is_seed_content = 0).
--   3. Validate: the loaded row count must equal JSON_LENGTH(p_rows); otherwise SIGNAL and abort.
--   4. Atomic swap in ONE statement:
--          result -> result_prev, result_stage -> result, result_prev -> result_stage
--
-- Any error before step 4 leaves the live table untouched.
--
-- Row cap
-- -------
-- The inline tally covers 0..99999 rows. The driver population is the plant's part master, which is
-- the widest of the twelve shapes; a population above the cap fails the validation in step 3 and
-- leaves the previous snapshot in place rather than loading a partial one.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_part_matches_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_part_matches_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    -- 1. Clear the stage twin (also discards any partially loaded previous attempt).
    TRUNCATE TABLE visual_part_matches_result_stage;

    -- 2. Load the complete new result set into the stage twin.
    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_part_matches_result_stage (
        term,
        part_number,
        description,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        COALESCE(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Term'))), ''),
        COALESCE(JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].PartNumber'))), ''),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Description'))),
        UTC_TIMESTAMP(),
        0
    FROM (
        SELECT (ones.n + tens.n * 10 + hundreds.n * 100 + thousands.n * 1000 + ten_thousands.n * 10000) AS i
        FROM (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS ones
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS tens
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS hundreds
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS thousands
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS ten_thousands
    ) AS n
    WHERE n.i < v_expected;

    -- 3. Validate the load before it can become visible.
    SELECT COUNT(*) INTO v_loaded FROM visual_part_matches_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_part_matches_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    -- 4. Atomic swap — the live name always resolves to a complete snapshot.
    RENAME TABLE visual_part_matches_result       TO visual_part_matches_result_prev,
                 visual_part_matches_result_stage TO visual_part_matches_result,
                 visual_part_matches_result_prev  TO visual_part_matches_result_stage;
END $$

DELIMITER ;