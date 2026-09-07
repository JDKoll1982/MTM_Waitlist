-- Rollback: seed_mock_master_default
-- Engine: MySQL 5.7
-- Purpose: Remove all rows inserted by the mock master seed.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE mock_master_tables_registry;
TRUNCATE TABLE mock_parts;
TRUNCATE TABLE mock_work_orders;
TRUNCATE TABLE mock_work_centers;
TRUNCATE TABLE mock_locations;
TRUNCATE TABLE mock_requesters;
TRUNCATE TABLE mock_inventory_locations;
TRUNCATE TABLE mock_request_types;

SET FOREIGN_KEY_CHECKS = 1;
