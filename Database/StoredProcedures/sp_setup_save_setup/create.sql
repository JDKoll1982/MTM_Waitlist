-- Stored Procedure: sp_setup_save_setup
-- Engine: MySQL 5.7
-- Purpose: Persist/replace workstation setup state and write history snapshots.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_save_setup;

DELIMITER $$

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
BEGIN
	DECLARE v_now_utc DATETIME;
	DECLARE v_existing_active_job_id BIGINT DEFAULT NULL;
	DECLARE v_result_active_job_id BIGINT DEFAULT NULL;

	IF p_work_order IS NULL OR TRIM(p_work_order) = '' THEN
		SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'work_order is required';
	END IF;

	IF p_part_number IS NULL OR TRIM(p_part_number) = '' THEN
		SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'part_number is required';
	END IF;

	IF p_sequence_number IS NULL OR TRIM(p_sequence_number) = '' THEN
		SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'sequence_number is required';
	END IF;

	IF p_work_center IS NULL OR TRIM(p_work_center) = '' THEN
		SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'work_center is required';
	END IF;

	SET v_now_utc = UTC_TIMESTAMP();

	START TRANSACTION;

	SELECT id
	  INTO v_existing_active_job_id
	  FROM setup_active_jobs
	 WHERE work_center = TRIM(p_work_center)
	 LIMIT 1
	 FOR UPDATE;

	IF v_existing_active_job_id IS NULL THEN
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
		) VALUES (
			UUID(),
			TRIM(p_work_order),
			TRIM(p_part_number),
			TRIM(p_sequence_number),
			TRIM(p_work_center),
			NULLIF(TRIM(p_selected_dunnage_type_id), ''),
			NULLIF(TRIM(p_selected_dunnage_part_id), ''),
			p_subordinate_parts_json,
			p_selected_dunnage_parts_json,
			1,
			p_saved_by_user_id,
			p_saved_by_user_id,
			v_now_utc,
			v_now_utc
		);

		SET v_result_active_job_id = LAST_INSERT_ID();
	ELSE
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
			id,
			'replaced',
			work_order,
			part_number,
			sequence_number,
			work_center,
			selected_dunnage_type_id,
			selected_dunnage_part_id,
			subordinate_parts_json,
			selected_dunnage_parts_json,
			p_saved_by_user_id,
			v_now_utc
		  FROM setup_active_jobs
		 WHERE id = v_existing_active_job_id;

		UPDATE setup_active_jobs
		   SET work_order = TRIM(p_work_order),
			   part_number = TRIM(p_part_number),
			   sequence_number = TRIM(p_sequence_number),
			   selected_dunnage_type_id = NULLIF(TRIM(p_selected_dunnage_type_id), ''),
			   selected_dunnage_part_id = NULLIF(TRIM(p_selected_dunnage_part_id), ''),
			   subordinate_parts_json = p_subordinate_parts_json,
			   selected_dunnage_parts_json = p_selected_dunnage_parts_json,
			   is_active = 1,
			   updated_by_user_id = p_saved_by_user_id,
			   updated_utc = v_now_utc
		 WHERE id = v_existing_active_job_id;

		SET v_result_active_job_id = v_existing_active_job_id;
	END IF;

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
	) VALUES (
		UUID(),
		v_result_active_job_id,
		'saved',
		TRIM(p_work_order),
		TRIM(p_part_number),
		TRIM(p_sequence_number),
		TRIM(p_work_center),
		NULLIF(TRIM(p_selected_dunnage_type_id), ''),
		NULLIF(TRIM(p_selected_dunnage_part_id), ''),
		p_subordinate_parts_json,
		p_selected_dunnage_parts_json,
		p_saved_by_user_id,
		v_now_utc
	);

	COMMIT;

	SELECT v_result_active_job_id AS active_job_id;
END$$

DELIMITER;