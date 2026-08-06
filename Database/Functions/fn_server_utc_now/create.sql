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