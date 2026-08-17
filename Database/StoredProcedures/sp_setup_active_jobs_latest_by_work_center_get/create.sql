-- Stored Procedure: sp_setup_active_jobs_latest_by_work_center_get
-- Engine: MySQL 5.7
-- Purpose: Retrieve latest active setup row per work center.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_active_jobs_latest_by_work_center_get;

CREATE PROCEDURE sp_setup_active_jobs_latest_by_work_center_get()
SELECT aj.work_center, aj.work_order, aj.part_number, aj.sequence_number
FROM setup_active_jobs aj
INNER JOIN (
    SELECT work_center, MAX(id) AS max_id
    FROM setup_active_jobs
    WHERE is_active = 1
    GROUP BY work_center
) latest ON latest.max_id = aj.id;
