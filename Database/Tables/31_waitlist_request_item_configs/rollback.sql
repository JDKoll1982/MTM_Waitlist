-- Rollback: waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Reverses: Database/Tables/31_waitlist_request_item_configs/create.sql

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS waitlist_request_item_configs;

SET FOREIGN_KEY_CHECKS = 1;
