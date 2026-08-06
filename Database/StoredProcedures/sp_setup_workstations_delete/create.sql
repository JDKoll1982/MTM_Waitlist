-- Stored Procedure: sp_setup_workstations_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_delete;

CREATE PROCEDURE sp_setup_workstations_delete(
    IN p_workstation_id VARCHAR(32)
)
DELETE FROM setup_workstations_catalog
WHERE id = CAST(TRIM(p_workstation_id) AS UNSIGNED);