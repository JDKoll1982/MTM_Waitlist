-- Create procedure: sp_waitlist_defect_types_get_all
-- Engine: MySQL 5.7
-- Purpose: List active NCM defect types (for the worker picker and the Module_Settings editor),
--          ordered by sort_order then defect_name.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_defect_types_get_all;

CREATE PROCEDURE sp_waitlist_defect_types_get_all()
SELECT
    id,
    public_id,
    defect_name,
    description,
    sort_order,
    is_active,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
FROM waitlist_defect_types
WHERE is_active = 1
ORDER BY sort_order ASC, defect_name ASC;
