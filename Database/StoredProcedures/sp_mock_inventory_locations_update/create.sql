-- Create procedure: sp_mock_inventory_locations_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_inventory_locations_update;

CREATE PROCEDURE sp_mock_inventory_locations_update(
    IN p_id BIGINT,
    IN p_part_number VARCHAR(50),
    IN p_location_code VARCHAR(50),
    IN p_on_hand_quantity DECIMAL(18,2)
)
UPDATE mock_inventory_locations
SET part_number = NULLIF(TRIM(p_part_number), ''), location_code = NULLIF(TRIM(p_location_code), ''), on_hand_quantity = p_on_hand_quantity, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

