-- Rollback for table: ops_request_types_catalog

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS ops_request_types_catalog;

SET FOREIGN_KEY_CHECKS = 1;
