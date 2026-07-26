-- Server UTC function for startup session validation
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_server_utc_now;

DELIMITER $$

CREATE FUNCTION fn_server_utc_now()
RETURNS DATETIME
DETERMINISTIC
NO SQL
BEGIN
    RETURN UTC_TIMESTAMP();
END$$

DELIMITER;