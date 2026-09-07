-- Create procedure: sp_mock_inventory_locations_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_get;

CREATE PROCEDURE sp_mock_inventory_locations_get()
SELECT id, public_id, part_number, location_code, on_hand_quantity, created_utc, updated_utc
FROM mock_inventory_locations
ORDER BY id ASC;

