-- Create procedure: sp_mock_request_types_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_request_types_get;

CREATE PROCEDURE sp_mock_request_types_get()
SELECT id, public_id, request_type, subtype, is_active, created_utc, updated_utc
FROM mock_request_types
ORDER BY id ASC;

