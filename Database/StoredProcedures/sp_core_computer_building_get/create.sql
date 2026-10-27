-- Stored Procedure: sp_core_computer_building_get
-- Engine: MySQL 5.7
--
-- Purpose: the building one computer stands in, which is the answer every building chooser opens on.
--
-- Contract:
--   IN  p_computer_name  VARCHAR(128)  a registry `computer_name` or its normalised hostname
--   OUT building_name, or no row when the computer has never been set up
--
-- No row is an answer rather than a failure: a computer that has never been configured has no building, and the
-- caller falls back to the plant default. Returning the default here would make "never asked" indistinguishable
-- from "answered Expo Drive".

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computer_building_get;

CREATE PROCEDURE sp_core_computer_building_get(
    IN p_computer_name VARCHAR(128)
)
SELECT
    cb.building_name
FROM core_computer_buildings cb
INNER JOIN core_computers_registry r ON r.id = cb.computer_id
WHERE r.computer_name = p_computer_name
   OR r.hostname_normalized = p_computer_name
ORDER BY r.id ASC
LIMIT 1;
