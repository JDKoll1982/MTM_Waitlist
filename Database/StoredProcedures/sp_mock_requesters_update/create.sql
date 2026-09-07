-- Create procedure: sp_mock_requesters_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_update;

CREATE PROCEDURE sp_mock_requesters_update(
    IN p_id BIGINT,
    IN p_employee_number VARCHAR(32),
    IN p_display_name VARCHAR(128)
)
UPDATE mock_requesters
SET employee_number = NULLIF(TRIM(p_employee_number), ''), display_name = NULLIF(TRIM(p_display_name), ''), updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

