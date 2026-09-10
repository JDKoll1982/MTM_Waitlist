-- ============================================================
-- Stored Procedure: sp_visual_work_order_lookup_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (reference refresh pattern, shape 1 of 5)
-- Created:    2026-09-09
-- Feature:    001-module-mock-visual-fallback (task T015)
-- Shape key:  work_order_lookup
-- ============================================================
--
-- Purpose
-- -------
-- Replace the live mirror `visual_work_order_lookup_result` with a complete new result set,
-- atomically. Readers address only the live table name, so they always observe either the
-- complete previous snapshot or the complete new one — never a partial set and never an empty
-- table (FR-006, SC-005). See research.md R1 and data-model.md §3.6.
--
-- Contract (design A — confirmed 2026-09-09)
-- ------------------------------------------
-- The Infor Visual read is external (SQL Server) and the caller owns the driver inputs, so the
-- SERVICE produces the complete result and hands it to this procedure. The procedure owns the
-- swap so the atomic multi-table RENAME stays in the database (constitution III, SP-first).
-- The service cannot INSERT the stage rows itself: C# may not contain MySQL statement text
-- (inline-SQL audit, task T098).
--
--   p_rows : JSON array of result objects. Keys are the live read's projection names:
--              "NormalizedWorkOrder" (the read's input key)
--              "PartNumber", "Description", "WorkCenter"
--            A NULL or empty array produces an empty snapshot, which is a legitimate result
--            (an empty live read is a valid answer — FR-024).
--
-- Steps
-- -----
--   1. TRUNCATE the stage twin.
--   2. Expand p_rows into the stage twin (refreshed_utc = UTC_TIMESTAMP(), is_seed_content = 0).
--   3. Validate: the loaded row count must equal JSON_LENGTH(p_rows); otherwise SIGNAL and abort.
--   4. Atomic swap in ONE statement (documented left-to-right three-name idiom; the middle step's
--      target name is temporarily occupied by the outgoing snapshot):
--          result -> result_prev, result_stage -> result, result_prev -> result_stage
--      The transient `_prev` name never persists between cycles.
--
-- Failure behaviour (FR-006)
-- --------------------------
-- Any error before step 4 (bad payload, NOT NULL violation, validation mismatch) aborts the
-- procedure with the live table UNTOUCHED. A partially loaded stage twin is discarded by the
-- TRUNCATE at the start of the next cycle. MySQL documents RENAME TABLE as all-or-nothing:
-- "If any errors occur during a RENAME TABLE, the statement fails and no changes are made."
--
-- Row cap
-- -------
-- The inline tally covers 0..1999 rows, which bounds the JSON expansion without a helper table
-- (data-model.md §11: mtm_mock carries no table other than the mirrors and their stage twins).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_work_order_lookup_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_work_order_lookup_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    -- 1. Clear the stage twin (also discards any partially loaded previous attempt).
    TRUNCATE TABLE visual_work_order_lookup_result_stage;

    -- 2. Load the complete new result set into the stage twin.
    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_work_order_lookup_result_stage (
        normalized_work_order,
        part_number,
        description,
        work_center,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].NormalizedWorkOrder'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].PartNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].Description'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].WorkCenter'))),
        UTC_TIMESTAMP(),
        0
    FROM (
        SELECT (ones.n + tens.n * 10 + hundreds.n * 100 + thousands.n * 1000) AS i
        FROM (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS ones
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS tens
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
              UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9) AS hundreds
        CROSS JOIN (SELECT 0 AS n UNION ALL SELECT 1) AS thousands
    ) AS n
    WHERE n.i < v_expected;

    -- 3. Validate the load before it can become visible.
    SELECT COUNT(*) INTO v_loaded FROM visual_work_order_lookup_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_work_order_lookup_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    -- 4. Atomic swap — the live name always resolves to a complete snapshot.
    RENAME TABLE visual_work_order_lookup_result       TO visual_work_order_lookup_result_prev,
                 visual_work_order_lookup_result_stage TO visual_work_order_lookup_result,
                 visual_work_order_lookup_result_prev  TO visual_work_order_lookup_result_stage;
END $$

DELIMITER ;
