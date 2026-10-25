-- ============================================================
-- Stored Procedure: sp_visual_work_order_search_get
-- Database:   mtm_mock
-- Engine:     MySQL 5.7 (verified on 9.6)
-- Kind:       StoredProcedure / create  (read, shape 8 of 8)
-- Created:    2026-10-21
-- Feature:    001-module-mock-visual-fallback (read shape 8, playbook step 3)
-- Shape key:  work_order_search
-- ============================================================
--
-- Parameterized read of the LIVE mirror. Returns exactly the column shape the live Infor Visual
-- search returns (FR-004), so a caller cannot distinguish cached from live:
--   live : WorkOrder, PartNumber, PartDescription, CustomerName, WorkCenter, Status, WantDate
--   here : same names/order, projected from the mirror's own columns, with the same matching rules.
--
-- The matching is copied from GetWorkOrderSearch.sql, not re-invented: the typed work-order text is
-- matched as typed AND with its `WO-` prefix and dashes removed (so `0752` finds `WO-0752xx`), the
-- part and customer filters are substring matches, and rows are ranked exact -> prefix -> contains
-- before the newest want date. Two sources that matched differently would make the cached answer a
-- different answer, which is exactly what FR-004 forbids.
--
-- Before the first successful refresh the mirror is empty, and an empty result is a valid answer for
-- a search (see the mirror table's header).
-- ============================================================

USE mtm_mock;

DROP PROCEDURE IF EXISTS sp_visual_work_order_search_get;

DELIMITER $$

CREATE PROCEDURE sp_visual_work_order_search_get(
    IN p_work_order_text VARCHAR(60),
    IN p_part_filter VARCHAR(60),
    IN p_customer_filter VARCHAR(120)
)
BEGIN
    DECLARE v_work_order_text VARCHAR(60);
    DECLARE v_part_filter VARCHAR(60);
    DECLARE v_customer_filter VARCHAR(120);
    DECLARE v_digits VARCHAR(60);
    DECLARE v_candidate VARCHAR(30);

    SET v_work_order_text = TRIM(COALESCE(p_work_order_text, ''));
    SET v_part_filter = TRIM(COALESCE(p_part_filter, ''));
    SET v_customer_filter = TRIM(COALESCE(p_customer_filter, ''));
    SET v_digits = REPLACE(REPLACE(REPLACE(UPPER(v_work_order_text), 'WO-', ''), 'WO', ''), '-', '');
    SET v_candidate = CASE
        WHEN v_digits <> '' AND CHAR_LENGTH(v_digits) <= 6 AND v_digits NOT REGEXP '[^0-9]'
            THEN CONCAT('WO-', LPAD(v_digits, 6, '0'))
        ELSE ''
    END;

    SELECT
        search.work_order AS WorkOrder,
        search.part_number AS PartNumber,
        COALESCE(search.part_description, '') AS PartDescription,
        COALESCE(search.customer_name, '') AS CustomerName,
        COALESCE(search.work_center, '') AS WorkCenter,
        COALESCE(search.status, '') AS Status,
        search.want_date AS WantDate
    FROM visual_work_order_search_result AS search
    WHERE
        (
            v_work_order_text = ''
            OR search.work_order LIKE CONCAT('%', v_work_order_text, '%')
            OR (v_digits <> '' AND search.work_order LIKE CONCAT('%', v_digits, '%'))
        )
        AND (v_part_filter = '' OR search.part_number LIKE CONCAT('%', v_part_filter, '%'))
        AND (v_customer_filter = '' OR COALESCE(search.customer_name, '') LIKE CONCAT('%', v_customer_filter, '%'))
    ORDER BY
        CASE
            WHEN v_candidate <> '' AND search.work_order = v_candidate THEN 0
            WHEN v_candidate <> '' AND search.work_order LIKE CONCAT(v_candidate, '%') THEN 1
            WHEN v_digits <> '' AND search.work_order LIKE CONCAT('%', v_digits, '%') THEN 2
            ELSE 3
        END,
        search.want_date,
        search.work_order DESC
    LIMIT 200;
END $$

DELIMITER ;
