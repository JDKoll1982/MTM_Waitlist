-- Rollback: seed_waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_waitlist_request_item_configs/create.sql
-- Clears the seeded rows. The table itself is dropped by
-- Database/Tables/31_waitlist_request_item_configs/rollback.sql, which owns its existence.

USE mtm_waitlist;

TRUNCATE TABLE waitlist_request_item_configs;
