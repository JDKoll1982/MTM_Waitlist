-- Stored Procedure: sp_setup_job_history_by_work_center_get
-- Engine: MySQL 5.7
-- Purpose: Get the most recent setup history events recorded for one work center, newest first.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_by_work_center_get;

CREATE PROCEDURE sp_setup_job_history_by_work_center_get(
    IN p_work_center VARCHAR(64)
)
SELECT
    h.event_action,
    h.work_order,
    h.part_number,
    h.sequence_number,
    h.work_center,
    h.changed_utc
FROM setup_job_history h
WHERE h.work_center = TRIM(p_work_center)
ORDER BY h.changed_utc DESC, h.id DESC
LIMIT 200;
