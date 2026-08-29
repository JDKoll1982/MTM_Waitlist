-- View: vw_setup_work_centers_active
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP VIEW IF EXISTS vw_setup_work_centers_active;

CREATE VIEW vw_setup_work_centers_active AS
SELECT
    id,
    public_id,
    building,
    work_center_name,
    is_active,
    sort_rank,
    created_utc,
    updated_utc
FROM setup_work_centers_catalog
WHERE
    is_active = 1;