-- Seed: seed_mock_master_default
-- Engine: MySQL 5.7
-- Purpose: Populate the Developer-editable mock master tables (registry + all mock_* tables) with
--          realistic values so the New-Request mock flows and the location/inventory grids have data.
-- Rerunnable: truncates each mock table before inserting.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

-- ---------------------------------------------------------------
-- mock_master_tables_registry
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_master_tables_registry;

INSERT INTO mock_master_tables_registry
    (public_id, table_name, ui_display_name, description_text, is_active, sort_rank, created_utc, updated_utc)
VALUES
    (UUID(), 'mock_parts', 'Mock Parts',
     'The list of part numbers the app uses for testing. Coils start with MMC, flatstock with MMF, and Other items use a 23- code. Edit the part number and description here.', 1, 10, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_work_orders', 'Mock Work Orders',
     'The jobs/work orders the app can attach a request to. Each job runs a part on a work center and may have a coil loaded on it.', 1, 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_work_centers', 'Mock Work Centers',
     'The presses/work centers (by their plant code, e.g. 100-3) where requests are made.', 1, 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_locations', 'Mock Inventory Locations',
     'Where material is stored on the floor (plant location codes). Some locations are marked as ignored and are hidden from lists and totals.', 1, 40, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_requesters', 'Mock Requesters',
     'The people (by employee number and name) who can submit requests during testing.', 1, 50, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_inventory_locations', 'Mock Inventory on Hand',
     'How much of each part is on hand at each location, shown as a combined weight in pounds.', 1, 60, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_request_types', 'Mock Request Types',
     'The kinds of requests that can be created (for example Bring a Coil, Pickup).', 1, 70, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_parts
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_parts;

INSERT INTO mock_parts
    (public_id, part_number, part_category, part_description, unit_of_measure, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'MMC0001000', 'Coil', 'Primary coil', 'lb', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0000365', 'Coil', 'General coil', 'lb', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMF0005008', 'Flatstock', 'Flat stock sheet', 'each', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '23-0001-0001', 'Other', 'Other item one', 'each', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '23-0002-0002', 'Other', 'Other item two', 'each', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_work_orders
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_work_orders;

INSERT INTO mock_work_orders
    (public_id, work_order_number, part_number, work_center_code, is_coil_bearing, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'WO-076951', 'MMC0001000', '100-3', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'WO-076952', 'MMC0000365', '100-6', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'WO-076953', 'MMF0005008', '100-8', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_work_centers
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_work_centers;

INSERT INTO mock_work_centers
    (public_id, work_center_code, work_center_description, building, is_active, created_utc, updated_utc)
VALUES
    (UUID(), '100-3', 'Coil Press 100-3', 'Expo Drive', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '100-6', 'Coil Press 100-6', 'Expo Drive', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '100-8', 'Press 100-8', 'Expo Drive', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_locations
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_locations;

INSERT INTO mock_locations
    (public_id, location_code, location_description, is_ignored, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'V-A0-01', 'Active storage location A0-01', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-A0-04', 'Active storage location A0-04', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-B2-10', 'Active storage location B2-10', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-A0-00', 'Empty location A0-00', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'WC', 'Work cell location (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'NCM', 'Non-conforming material (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-WC', 'Work cell variant (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'NCM-VITS', 'Non-conforming VITS (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'SHIP', 'Shipping location (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_requesters
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_requesters;

INSERT INTO mock_requesters
    (public_id, employee_number, display_name, is_active, created_utc, updated_utc)
VALUES
    (UUID(), '6229', 'John Koll', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '5000', 'Other User', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_inventory_locations
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_inventory_locations;

INSERT INTO mock_inventory_locations
    (public_id, part_number, location_code, on_hand_quantity, created_utc, updated_utc)
VALUES
    (UUID(), 'MMC0001000', 'V-A0-01', 15000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-A0-04', 25000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-B2-10', 6000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'WC', 2000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'NCM', 1000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-WC', 500, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'NCM-VITS', 1500, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'SHIP', 4000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-A0-00', 0, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMF0005008', 'V-A0-01', 12000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMF0005008', 'V-A0-04', 8000, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- ---------------------------------------------------------------
-- mock_request_types
-- ---------------------------------------------------------------
TRUNCATE TABLE mock_request_types;

INSERT INTO mock_request_types
    (public_id, request_type, subtype, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'Coil', 'Bring', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Coil', 'Pickup', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Pickup', 'Pickup Coil', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Pickup', 'Pickup Flatstock', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Other', NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Scrap', NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

SET FOREIGN_KEY_CHECKS = 1;
