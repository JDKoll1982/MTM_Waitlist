-- Create procedure: sp_mock_inventory_locations_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_insert;

CREATE PROCEDURE sp_mock_inventory_locations_insert(
    IN p_part_number VARCHAR(50),
    IN p_location_code VARCHAR(50),
    IN p_on_hand_quantity DECIMAL(18,2)
)
INSERT INTO mock_inventory_locations (part_number, location_code, on_hand_quantity, public_id, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_part_number), ''), NULLIF(TRIM(p_location_code), ''), p_on_hand_quantity, UUID(), UTC_TIMESTAMP(), UTC_TIMESTAMP());

