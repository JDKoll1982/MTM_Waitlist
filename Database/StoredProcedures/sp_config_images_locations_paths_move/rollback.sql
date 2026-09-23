-- Rollback: sp_config_images_locations_paths_move
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T021)
--
-- Drops the rewrite procedure. It owns no data of its own: the rows it rewrote are ordinary rows in
-- config_images_locations, and putting their values back is the paired seed's rollback
-- (Seeds/seed_picture_layout_move/rollback.sql), which rewrites each value from the same table of pairs.
--
-- Dropping rather than keeping it is the honest reversal. A leftover rewrite procedure is a bulk mutation of every
-- picture path in the store with no caller, and the first thing anyone would do with it is run it.
--
-- The order matters when reversing this feature: run this drop first, then the seed's rollback, so no value is
-- moved while the only thing that knows the pairs has already been removed.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_paths_move;
