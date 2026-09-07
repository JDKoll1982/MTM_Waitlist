-- Create procedure: sp_mock_request_types_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_update;

CREATE PROCEDURE sp_mock_request_types_update(
    IN p_id BIGINT,
    IN p_request_type VARCHAR(64),
    IN p_subtype VARCHAR(128)
)
UPDATE mock_request_types
SET request_type = NULLIF(TRIM(p_request_type), ''), subtype = NULLIF(TRIM(p_subtype), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

