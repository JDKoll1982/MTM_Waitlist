-- Rollback: sp_machine_configuration_reset
-- Engine: MySQL 5.7
-- Feature: 010-startup-rebuild (task T075)
-- Drops the writer. It owns no data of its own: it edits rows that belong to `core_computers_registry`,
-- `config_images_locations` and `config_settings_values`, and each of those rows is created and removed by its
-- own table's artifacts. A store reversed here keeps whatever configuration it held, which is the honest state
-- for a reversal to leave: the reset is an operation, not a record.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_machine_configuration_reset;
