-- Rollback: seed_waitlist_request_catalog
-- Engine: MySQL 5.7
-- Purpose: Remove all rows inserted by the waitlist request-type catalog seed.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE waitlist_request_subtypes;
TRUNCATE TABLE waitlist_request_types;

SET FOREIGN_KEY_CHECKS = 1;
