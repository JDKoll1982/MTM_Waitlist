-- Rollback: sp_config_images_locations_paths_move
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T021)
--
-- Drops the rewrite procedure. It owns no data of its own: the rows it rewrote are ordinary rows in
-- config_images_locations, put back from the same table of pairs by the paired seed's rollback
-- (Seeds/seed_picture_layout_move/rollback.sql). **That seed has since been retired**, with its block in
-- AllSeeds.sql and its helper procedure dropped, so this artifact's only remaining reversal is its own drop; the
-- row values it once rewrote as a promoted step are now the hand-run move's business
-- (tools/Move-PartPictureLayout.ps1), which reverses them for the same reason it moved them.
--
-- Dropping rather than keeping it is the honest reversal. A leftover rewrite procedure is a bulk mutation of every
-- picture path in the store with no caller, and the first thing anyone would do with it is run it.
--
-- The order matters when reversing this feature: run this drop first, then the recorded-path reversal, so no value
-- is moved while the only thing that knows the pairs has already been removed.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_paths_move;
