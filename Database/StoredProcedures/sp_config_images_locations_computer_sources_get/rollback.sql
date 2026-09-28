-- Rollback: sp_config_images_locations_computer_sources_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T051)
-- Drops the reader. It owns no data and changes nothing it reads.
--
-- **Its create.sql was withdrawn by task T198** (FR-040), so no artifact in this tree creates it any more and
-- this drop is the whole of what is left. Nothing in the application ever called it: it was written for the
-- machine-setup screen, which no longer captures a folder, and the machine-configuration service reads the
-- registry row alone. Its body was removed from `Database/StoredProcedures/AllSPs.sql` in the same change, and
-- the withdrawal is recorded in `Database/Bootstrap/update_table_descriptions.sql`.
--
-- Running this drop is the store side of the retirement: a store installed before T198 still holds the routine.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_get;
