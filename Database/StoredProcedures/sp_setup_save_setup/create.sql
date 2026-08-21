-- Stored Procedure: sp_setup_save_setup
-- Engine: MySQL 5.7
-- Purpose: Persist workstation setup state.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_save_setup;

CREATE PROCEDURE sp_setup_save_setup(
	IN p_work_order VARCHAR(32),
	IN p_part_number VARCHAR(64),
	IN p_sequence_number VARCHAR(32),
	IN p_work_center VARCHAR(64),
	IN p_selected_dunnage_type_id VARCHAR(64),
	IN p_selected_dunnage_part_id VARCHAR(64),
	IN p_subordinate_parts_json JSON,
	IN p_selected_dunnage_parts_json JSON,
	IN p_saved_by_user_id BIGINT
)
INSERT INTO setup_active_jobs (
	public_id,
	work_order,
	part_number,
	sequence_number,
	work_center,
	selected_dunnage_type_id,
	selected_dunnage_part_id,
	subordinate_parts_json,
	selected_dunnage_parts_json,
	is_active,
	created_by_user_id,
	updated_by_user_id,
	created_utc,
	updated_utc
)
SELECT
	UUID(),
	TRIM(p_work_order),
	TRIM(p_part_number),
	TRIM(p_sequence_number),
	TRIM(p_work_center),
	NULLIF(TRIM(p_selected_dunnage_type_id) COLLATE utf8mb4_unicode_ci, ''),
	NULLIF(TRIM(p_selected_dunnage_part_id) COLLATE utf8mb4_unicode_ci, ''),
	p_subordinate_parts_json,
	p_selected_dunnage_parts_json,
	1,
	p_saved_by_user_id,
	p_saved_by_user_id,
	UTC_TIMESTAMP(),
	UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
	work_order = VALUES(work_order),
	part_number = VALUES(part_number),
	sequence_number = VALUES(sequence_number),
	selected_dunnage_type_id = VALUES(selected_dunnage_type_id),
	selected_dunnage_part_id = VALUES(selected_dunnage_part_id),
	subordinate_parts_json = VALUES(subordinate_parts_json),
	selected_dunnage_parts_json = VALUES(selected_dunnage_parts_json),
	is_active = 1,
	updated_by_user_id = VALUES(updated_by_user_id),
	updated_utc = UTC_TIMESTAMP();