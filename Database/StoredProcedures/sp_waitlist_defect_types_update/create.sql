-- Create procedure: sp_waitlist_defect_types_update
-- Engine: MySQL 5.7
-- Purpose: Update an existing NCM defect type in waitlist_defect_types by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_update;

CREATE PROCEDURE sp_waitlist_defect_types_update(
    IN p_id BIGINT,
    IN p_defect_name VARCHAR(100),
    IN p_description VARCHAR(500),
    IN p_sort_order INT,
    IN p_updated_by_user_id BIGINT
)
UPDATE waitlist_defect_types
SET defect_name = NULLIF(TRIM(p_defect_name), ''),
    description = NULLIF(TRIM(COALESCE(p_description, '')), ''),
    sort_order = COALESCE(p_sort_order, 0),
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;
