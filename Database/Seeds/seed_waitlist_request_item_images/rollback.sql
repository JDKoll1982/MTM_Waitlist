-- Rollback: seed_waitlist_request_item_images
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_waitlist_request_item_images/create.sql
-- Removes only the rows this seed owns: the two scopes it introduced. Work-centre and legacy override rows
-- are left alone, because this seed never created them.

USE mtm_waitlist;

DELETE FROM config_images_locations WHERE scope IN ('request_item', 'request_category');
