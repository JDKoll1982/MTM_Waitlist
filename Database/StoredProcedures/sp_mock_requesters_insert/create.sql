-- Create procedure: sp_mock_requesters_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_insert;

CREATE PROCEDURE sp_mock_requesters_insert(
    IN p_employee_number VARCHAR(32),
    IN p_display_name VARCHAR(128)
)
INSERT INTO mock_requesters (employee_number, display_name, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_employee_number), ''), NULLIF(TRIM(p_display_name), ''), UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

