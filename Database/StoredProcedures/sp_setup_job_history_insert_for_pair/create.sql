-- Stored Procedure: sp_setup_job_history_insert_for_pair
-- Engine: MySQL 5.7
-- Purpose: Insert a history snapshot for an exact work order/part/sequence pair from setup_active_jobs.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_job_history_insert_for_pair;

CREATE PROCEDURE sp_setup_job_history_insert_for_pair(
    IN p_event_action VARCHAR(32),
    IN p_changed_by_user_id BIGINT,
    IN p_work_order VARCHAR(32),
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32)
)
INSERT INTO setup_job_history (
    public_id,
    active_job_id,
    event_action,
    work_order,
    part_number,
    sequence_number,
    work_center,
    selected_dunnage_type_id,
    selected_dunnage_part_id,
    subordinate_parts_json,
    selected_dunnage_parts_json,
    changed_by_user_id,
    changed_utc
)
SELECT
    UUID(),
    aj.id,
    TRIM(p_event_action),
    aj.work_order,
    aj.part_number,
    aj.sequence_number,
    aj.work_center,
    aj.selected_dunnage_type_id,
    aj.selected_dunnage_part_id,
    aj.subordinate_parts_json,
    aj.selected_dunnage_parts_json,
    p_changed_by_user_id,
    UTC_TIMESTAMP()
FROM setup_active_jobs aj
WHERE aj.work_order = TRIM(p_work_order)
  AND aj.part_number = TRIM(p_part_number)
  AND aj.sequence_number = TRIM(p_sequence_number)
ORDER BY aj.updated_utc DESC
LIMIT 1;
