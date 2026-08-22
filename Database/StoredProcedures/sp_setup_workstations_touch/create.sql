-- Stored Procedure: sp_setup_workstations_touch
-- Engine: MySQL 5.7
-- Purpose: Refresh the work center catalog "Last Updated" timestamp after setup activity.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_touch;

CREATE PROCEDURE sp_setup_workstations_touch(
    IN p_work_center VARCHAR(64),
    IN p_updated_by_user_id BIGINT
)
UPDATE setup_workstations_catalog
SET
    updated_utc = UTC_TIMESTAMP(),
    updated_by_user_id = COALESCE(p_updated_by_user_id, updated_by_user_id)
WHERE workstation_name = TRIM(p_work_center);
