-- Rollback for fn_server_utc_now

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_server_utc_now;