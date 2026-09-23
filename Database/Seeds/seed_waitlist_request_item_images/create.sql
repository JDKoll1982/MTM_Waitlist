-- Seed: seed_waitlist_request_item_images
-- Engine: MySQL 5.7
-- Purpose: retire the item pictures this seed used to write.
--
-- What it used to do, and why it cannot do it any more:
--   It seeded an Item's own picture for six Items and a Category family picture for three Categories, every
--   one of them pointing at `Assets/RequestTypes/*.png`. Those files were 68-byte single-pixel stand-ins for
--   artwork that was never delivered, and the folder is gone from the repository altogether. Every one of the
--   rows therefore resolved to the "no image" placeholder, so what the store held was a claim with nothing
--   behind it: the picture screen showed a custom image that was not there, and the row's only real effect was
--   to say "this Item has a picture" when it had none.
--
-- What is seeded now: nothing.
--   An Item's picture is a setting an operator makes through the picture screen, and the application's own
--   answer to "nothing configured" is the one no-image placeholder. A seeded row could only ever duplicate
--   that answer while pretending to be a picture, which is the lie this seed's own header used to warn about.
--
-- What the delete is for:
--   A store that ran an earlier version of this seed still holds those rows. They are removed by the one thing
--   that identifies them — the folder that never shipped — so an operator's own choice under either scope is
--   left exactly where it is, and re-running is both idempotent and non-destructive. (The earlier version
--   cleared both scopes outright before re-inserting; that is no longer true and no longer wanted.)
--
-- Reversal: see rollback.sql. There is nothing to put back.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM config_images_locations WHERE image_path LIKE 'Assets/RequestTypes/%';

SET FOREIGN_KEY_CHECKS = 1;
