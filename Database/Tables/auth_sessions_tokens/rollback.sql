-- Rollback for table: auth_sessions_tokens

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS auth_sessions_tokens;

SET FOREIGN_KEY_CHECKS = 1;