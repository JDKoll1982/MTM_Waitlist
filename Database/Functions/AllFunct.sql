-- Create function: fn_config_settings_scope_rank
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_config_settings_scope_rank;

CREATE FUNCTION fn_config_settings_scope_rank(p_scope_type VARCHAR(16))
RETURNS TINYINT
DETERMINISTIC
RETURN CASE LOWER(TRIM(p_scope_type))
    WHEN 'workstation' THEN 1
    WHEN 'all_users' THEN 2
    WHEN 'user' THEN 3
    WHEN 'admin' THEN 4
    WHEN 'developer' THEN 5
    ELSE 0
END;

-- Server UTC function for startup session validation
-- Engine: MySQL 5.7
-- phpMyAdmin: run with default Delimiter ;

USE mtm_waitlist;

DROP FUNCTION IF EXISTS `fn_server_utc_now`;

CREATE FUNCTION `fn_server_utc_now`()
RETURNS DATETIME
DETERMINISTIC
NO SQL
SQL SECURITY DEFINER
RETURN UTC_TIMESTAMP();

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

