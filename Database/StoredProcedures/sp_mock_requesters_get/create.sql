-- Create procedure: sp_mock_requesters_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_requesters_get;

CREATE PROCEDURE sp_mock_requesters_get()
SELECT id, public_id, employee_number, display_name, is_active, created_utc, updated_utc
FROM mock_requesters
ORDER BY id ASC;

