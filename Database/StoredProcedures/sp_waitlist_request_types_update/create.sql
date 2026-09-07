-- Create procedure: sp_waitlist_request_types_update
-- Engine: MySQL 5.7
-- Purpose: Update a real (non-mock) request type in waitlist_request_types by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_update;

CREATE PROCEDURE sp_waitlist_request_types_update(
    IN p_id BIGINT,
    IN p_request_type VARCHAR(64),
    IN p_control VARCHAR(255),
    IN p_flow VARCHAR(64),
    IN p_requires_text_input TINYINT,
    IN p_prompt_text VARCHAR(500),
    IN p_min_length INT,
    IN p_max_length INT,
    IN p_default_image_path VARCHAR(500),
    IN p_center_data_grid_fields_json JSON,
    IN p_is_active TINYINT
)
UPDATE waitlist_request_types
SET request_type = NULLIF(TRIM(p_request_type), ''),
    control = NULLIF(TRIM(p_control), ''),
    flow = NULLIF(TRIM(COALESCE(p_flow, 'direct-to-confirmation')), ''),
    requires_text_input = COALESCE(p_requires_text_input, 0),
    prompt_text = NULLIF(TRIM(COALESCE(p_prompt_text, '')), ''),
    min_length = COALESCE(p_min_length, 0),
    max_length = COALESCE(p_max_length, 200),
    default_image_path = NULLIF(TRIM(COALESCE(p_default_image_path, '')), ''),
    center_data_grid_fields_json = p_center_data_grid_fields_json,
    is_active = COALESCE(p_is_active, 1),
    updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;
