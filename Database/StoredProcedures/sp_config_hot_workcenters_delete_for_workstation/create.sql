-- Stored Procedure: sp_config_hot_workcenters_delete_for_workstation
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_delete_for_workstation;

CREATE PROCEDURE sp_config_hot_workcenters_delete_for_workstation(
    IN p_core_workstation_id BIGINT
)
DELETE FROM config_workstation_hot_workcenters
WHERE core_workstation_id = p_core_workstation_id;
