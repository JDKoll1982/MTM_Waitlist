-- Create function: fn_config_settings_scope_rank
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_config_settings_scope_rank;

DELIMITER $$

CREATE FUNCTION fn_config_settings_scope_rank(p_scope_type VARCHAR(16))
RETURNS TINYINT
DETERMINISTIC
NO SQL
BEGIN
    RETURN CASE LOWER(TRIM(p_scope_type))
        WHEN 'workstation' THEN 1
        WHEN 'all_users' THEN 2
        WHEN 'user' THEN 3
        WHEN 'admin' THEN 4
        WHEN 'developer' THEN 5
        ELSE 0
    END;
END$$

DELIMITER;