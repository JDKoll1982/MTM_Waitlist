-- Stored Procedure: sp_config_hot_workcenters_get_for_workstation
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_hot_workcenters_get_for_workstation;

CREATE PROCEDURE sp_config_hot_workcenters_get_for_workstation(
    IN p_workstation_name VARCHAR(128)
)
SELECT
    swc.workstation_name AS work_center_name,
    cwhc.sort_rank
FROM config_workstation_hot_workcenters cwhc
INNER JOIN core_workstations_registry cwr ON cwr.id = cwhc.core_workstation_id
INNER JOIN setup_workstations_catalog swc ON swc.id = cwhc.setup_workstation_id
WHERE cwhc.is_active = 1
  AND swc.is_active = 1
  AND (
        cwr.workstation_name = TRIM(p_workstation_name)
        OR cwr.hostname_normalized = TRIM(p_workstation_name)
      )
ORDER BY cwhc.sort_rank ASC, swc.workstation_name ASC;
