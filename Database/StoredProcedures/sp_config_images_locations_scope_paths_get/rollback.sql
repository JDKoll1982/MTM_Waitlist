-- Rollback: sp_config_images_locations_scope_paths_get
-- Engine: MySQL 5.7
-- Feature: 008-part-pictures (task T013)
--
-- Drops the scope-paths read. It owns no data and no table: the rows it reads predate this feature, and the two
-- callers that depend on it are in the same change, so leaving it behind would be a read path with no reader.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_images_locations_scope_paths_get;
