-- Rollback: seed_waitlist_request_item_images
-- Engine: MySQL 5.7
-- Reverses: Database/Seeds/seed_waitlist_request_item_images/create.sql
--
-- There is nothing to put back. This seed used to write nine pictures pointing at `Assets/RequestTypes/*.png`,
-- files that were single-pixel stand-ins and no longer exist in the repository; restoring them would restore a
-- claim with nothing behind it. The seed and its reversal therefore leave the store in the same state: the
-- broken rows removed, and an operator's own choice under either scope untouched.
--
-- The statement is the same one `create.sql` runs, so this file is safe to execute on its own.

USE mtm_waitlist;

DELETE FROM config_images_locations WHERE image_path LIKE 'Assets/RequestTypes/%';
