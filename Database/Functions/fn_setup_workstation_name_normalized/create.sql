-- Function: fn_setup_workstation_name_normalized
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_setup_workstation_name_normalized;

CREATE FUNCTION fn_setup_workstation_name_normalized(
    p_workstation_name VARCHAR(64)
)
RETURNS VARCHAR(64)
DETERMINISTIC
RETURN TRIM(REPLACE(REPLACE(REPLACE(IFNULL(p_workstation_name, ''), '\r', ' '), '\n', ' '), '\t', ' '));