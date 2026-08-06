-- Stored Procedure: sp_setup_workstations_get_all
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_get_all;

CREATE PROCEDURE sp_setup_workstations_get_all()
SELECT
    id,
    workstation_name,
    is_active
FROM vw_setup_workstations_active
ORDER BY sort_rank ASC, workstation_name ASC;