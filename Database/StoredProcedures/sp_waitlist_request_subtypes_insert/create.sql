-- Create procedure: sp_waitlist_request_subtypes_insert
-- Engine: MySQL 5.7
-- Purpose: Insert a real (non-mock) request subtype into waitlist_request_subtypes.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_insert;

CREATE PROCEDURE sp_waitlist_request_subtypes_insert(
    IN p_request_type_id BIGINT,
    IN p_subtype_name VARCHAR(128),
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
INSERT INTO waitlist_request_subtypes (
    public_id, request_type_id, subtype_name, control, flow, requires_text_input, prompt_text,
    min_length, max_length, default_image_path, center_data_grid_fields_json,
    is_active, created_utc, updated_utc
)
VALUES (
    UUID(),
    p_request_type_id,
    NULLIF(TRIM(p_subtype_name), ''),
    NULLIF(TRIM(p_control), ''),
    NULLIF(TRIM(COALESCE(p_flow, 'direct-to-confirmation')), ''),
    COALESCE(p_requires_text_input, 0),
    NULLIF(TRIM(COALESCE(p_prompt_text, '')), ''),
    COALESCE(p_min_length, 0),
    COALESCE(p_max_length, 200),
    NULLIF(TRIM(COALESCE(p_default_image_path, '')), ''),
    p_center_data_grid_fields_json,
    COALESCE(p_is_active, 1),
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
);
