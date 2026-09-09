-- Create procedure: sp_waitlist_defect_types_insert
-- Engine: MySQL 5.7
-- Purpose: Insert a new active NCM defect type into waitlist_defect_types.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_insert;

CREATE PROCEDURE sp_waitlist_defect_types_insert(
    IN p_defect_name VARCHAR(100),
    IN p_description VARCHAR(500),
    IN p_sort_order INT,
    IN p_created_by_user_id BIGINT
)
INSERT INTO waitlist_defect_types (
    public_id, defect_name, description, sort_order, is_active,
    created_by_user_id, updated_by_user_id, created_utc, updated_utc
)
VALUES (
    UUID(),
    NULLIF(TRIM(p_defect_name), ''),
    NULLIF(TRIM(COALESCE(p_description, '')), ''),
    COALESCE(p_sort_order, 0),
    1,
    p_created_by_user_id,
    NULL,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);
