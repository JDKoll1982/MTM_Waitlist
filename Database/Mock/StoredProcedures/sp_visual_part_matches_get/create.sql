-- ============================================================
-- Stored Procedure: sp_visual_part_matches_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 9 of 12)
-- Created:    2026-11-15
-- Feature:    001-module-mock-visual-fallback (read shape 9, playbook step 3)
-- Shape key:  part_matches
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual read
-- returns, so a caller cannot distinguish cached from live (FR-004):
--   live : PartNumber, Description
--   here : same names and order, with the same matching and ordering rules.
--
-- The matching is copied from GetPartMatches.sql, not re-invented: the part whose number IS the term
-- first, then every part whose number CONTAINS it, capped at 200. The live script escapes the LIKE
-- metacharacters so a typed '%' or '_' is matched literally, and this procedure escapes them the same
-- way, using MySQL's own escape character, so the backslash is doubled in the literal.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_part_matches_get;

DELIMITER $$

CREATE PROCEDURE sp_visual_part_matches_get(
    IN p_term VARCHAR(190)
)
BEGIN
    DECLARE v_term VARCHAR(190);
    DECLARE v_pattern VARCHAR(580);

    SET v_term = TRIM(COALESCE(p_term, ''));
    SET v_pattern = CONCAT(
        '%',
        REPLACE(REPLACE(REPLACE(v_term, '\\', '\\\\'), '%', '\\%'), '_', '\\_'),
        '%');

    SELECT
        matches.part_number AS PartNumber,
        COALESCE(matches.description, '') AS Description
    FROM visual_part_matches_result AS matches
    WHERE
        v_term <> ''
        AND (matches.part_number = v_term OR matches.part_number LIKE v_pattern)
    ORDER BY
        CASE WHEN matches.part_number = v_term THEN 0 ELSE 1 END,
        matches.part_number
    LIMIT 200;
END $$

DELIMITER ;
