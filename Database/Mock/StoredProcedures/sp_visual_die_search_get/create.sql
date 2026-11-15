-- ============================================================
-- Stored Procedure: sp_visual_die_search_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 12 of 12)
-- Created:    2026-11-15
-- Feature:    001-module-mock-visual-fallback (read shape 12, playbook step 3)
-- Shape key:  die_search
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual read
-- returns, so a caller cannot distinguish cached from live (FR-004):
--   live : DieNumber, Description, Association, Location
--   here : same names and order, with the same association precedence and the same cap.
--
-- The term matches either the die's own number or the part the die is tied to, which is what the live
-- read's three relations between them cover. A die named more than once is reported once, under the
-- strongest association: Die before Explicit before History.
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_die_search_get;

DELIMITER $$

CREATE PROCEDURE sp_visual_die_search_get(
    IN p_term VARCHAR(190)
)
BEGIN
    DECLARE v_term VARCHAR(190);

    SET v_term = TRIM(COALESCE(p_term, ''));

    SELECT
        matched.die_number AS DieNumber,
        COALESCE(matched.description, '') AS Description,
        matched.association AS Association,
        COALESCE(matched.location, '') AS Location
    FROM visual_die_search_result AS matched
    INNER JOIN
    (
        SELECT
            candidates.die_number AS die_number,
            MIN(CASE candidates.association
                    WHEN 'Die' THEN 0
                    WHEN 'Explicit' THEN 1
                    ELSE 2
                END) AS strongest_rank
        FROM visual_die_search_result AS candidates
        WHERE
            v_term <> ''
            AND (candidates.term = v_term OR candidates.die_number = v_term)
        GROUP BY candidates.die_number
    ) AS strongest
        ON strongest.die_number = matched.die_number
        AND strongest.strongest_rank = CASE matched.association
                WHEN 'Die' THEN 0
                WHEN 'Explicit' THEN 1
                ELSE 2
            END
    WHERE
        v_term <> ''
        AND (matched.term = v_term OR matched.die_number = v_term)
    ORDER BY
        strongest.strongest_rank,
        matched.die_number
    LIMIT 200;
END $$

DELIMITER ;