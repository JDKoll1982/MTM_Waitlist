-- Create procedure: sp_waitlist_defect_types_delete
-- Engine: MySQL 5.7
-- Purpose: Remove an NCM defect type from waitlist_defect_types by id (hard delete; the managed
--          defect list is a small reference catalog, so removal is a true delete rather than a soft flag).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_delete;

CREATE PROCEDURE sp_waitlist_defect_types_delete(
    IN p_id BIGINT
)
DELETE FROM waitlist_defect_types
WHERE id = p_id;
