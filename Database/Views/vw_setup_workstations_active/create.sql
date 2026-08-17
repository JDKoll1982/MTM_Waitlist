-- View: vw_setup_workstations_active
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP VIEW IF EXISTS vw_setup_workstations_active;

CREATE VIEW vw_setup_workstations_active AS
SELECT
    id,
    public_id,
    building,
    workstation_name,
    is_active,
    sort_rank,
    created_utc,
    updated_utc
FROM setup_workstations_catalog
WHERE
    is_active = 1;