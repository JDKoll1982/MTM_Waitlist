-- ============================================================
-- Stored Procedure: sp_visual_employee_by_number_refresh
-- Database:   mtm_mock
-- Engine:     MySQL 5.7
-- Kind:       StoredProcedure / create  (refresh, shape 6 of 6)
-- Feature:    011-identifier-first-signin (task T008)
-- Shape key:  employee_by_number
-- ============================================================
--
-- Replace the live mirror `visual_employee_by_number_result` with a complete new result set,
-- atomically. Readers address only the live table name, so they always observe either the complete
-- previous snapshot or the complete new one, never a partial set and never an empty table.
--
--   p_rows : JSON array of result objects. Keys are the live read's projection names:
--              "EmployeeNumber" (the read's input key), "FirstName", "LastName"
--
-- A NULL or empty array produces an empty snapshot, which is a legitimate result: an empty live read
-- is a valid answer.
--
-- Steps: truncate the stage twin; expand p_rows into it; validate the loaded count against the
-- payload length; swap result -> prev, stage -> result, prev -> stage in one RENAME statement.
-- Any error before the swap aborts with the live table untouched.
--
-- The application never calls this procedure: refreshing the mirror is the on-host service's job
-- (FR-030), and this feature adds no refresh path to the launch.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_employee_by_number_refresh;

DELIMITER $$

CREATE PROCEDURE sp_visual_employee_by_number_refresh(
    IN p_rows JSON
)
BEGIN
    DECLARE v_expected INT DEFAULT 0;
    DECLARE v_loaded   INT DEFAULT 0;

    -- 1. Clear the stage twin (also discards any partially loaded previous attempt).
    TRUNCATE TABLE visual_employee_by_number_result_stage;

    -- 2. Load the complete new result set into the stage twin.
    SET v_expected = IFNULL(JSON_LENGTH(p_rows), 0);

    INSERT INTO visual_employee_by_number_result_stage (
        employee_number,
        first_name,
        last_name,
        refreshed_utc,
        is_seed_content
    )
    SELECT
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].EmployeeNumber'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].FirstName'))),
        JSON_UNQUOTE(JSON_EXTRACT(p_rows, CONCAT('$[', n.i, '].LastName'))),
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
    SELECT COUNT(*) INTO v_loaded FROM visual_employee_by_number_result_stage;

    IF v_loaded <> v_expected THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'sp_visual_employee_by_number_refresh: staged row count does not match payload length; live snapshot left untouched.';
    END IF;

    -- 4. Atomic swap: the live name always resolves to a complete snapshot.
    RENAME TABLE visual_employee_by_number_result       TO visual_employee_by_number_result_prev,
                 visual_employee_by_number_result_stage TO visual_employee_by_number_result,
                 visual_employee_by_number_result_prev  TO visual_employee_by_number_result_stage;
END $$

DELIMITER ;
