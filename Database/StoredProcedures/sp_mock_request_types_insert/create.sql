-- Create procedure: sp_mock_request_types_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_insert;

CREATE PROCEDURE sp_mock_request_types_insert(
    IN p_request_type VARCHAR(64),
    IN p_subtype VARCHAR(128)
)
INSERT INTO mock_request_types (request_type, subtype, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_request_type), ''), NULLIF(TRIM(p_subtype), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

