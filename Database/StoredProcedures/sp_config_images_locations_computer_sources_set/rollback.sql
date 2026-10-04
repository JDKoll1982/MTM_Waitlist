-- Rollback: sp_config_images_locations_computer_sources_set
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T051)
-- Drops the writer. It owns no data: the rows it wrote stay in `config_images_locations` as ordinary machine
-- configuration, and the table's own rollback is what removes them.
--
-- **Its create.sql was withdrawn by task T198** (FR-040), so no artifact in this tree creates it any more and
-- this drop is the whole of what is left. What retired it was the change of owner: the folders a computer reads
-- are held once for the plant, so nothing captures a machine's own and the machine-configuration service no
-- longer writes them. Its body was removed from `Database/StoredProcedures/AllSPs.sql` in the same change, and
-- the withdrawal is recorded in `Database/Bootstrap/update_table_descriptions.sql`.
--
-- Running this drop is the store side of the retirement: a store installed before T198 still holds the routine.
-- The rows a computer already holds are withdrawn by `Database/Seeds/seed_retire_computer_scope_picture_sources`,
-- which is a seed rather than a schema artifact because the rows are data.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_set;
