-- Stored Procedure: sp_setup_work_centers_get_all
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_work_centers_get_all;

CREATE PROCEDURE sp_setup_work_centers_get_all()
SELECT
    id,
    building,
    work_center_name,
    is_active,
    updated_utc
FROM vw_setup_work_centers_active
ORDER BY sort_rank ASC, work_center_name ASC;