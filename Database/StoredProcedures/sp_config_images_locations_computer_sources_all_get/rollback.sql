-- Rollback: sp_config_images_locations_computer_sources_all_get
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
-- Drops the reader. It owns no data: the rows it reads stay in `config_images_locations` as ordinary machine
-- configuration, and the table's own rollback is what removes them.
--
-- **Its create.sql was withdrawn by task T198** (FR-040), so no artifact in this tree creates it any more and
-- this drop is the whole of what is left. What retired it was its own readership: the machine-configuration
-- service was the only caller, and that service no longer reads a computer's own folders — where pictures come
-- from is held once for the plant. Its body was removed from `Database/StoredProcedures/AllSPs.sql` in the same
-- change, and the withdrawal is recorded in `Database/Bootstrap/update_table_descriptions.sql`.
--
-- Running this drop is the store side of the retirement: a store installed before T198 still holds the routine.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_computer_sources_all_get;
