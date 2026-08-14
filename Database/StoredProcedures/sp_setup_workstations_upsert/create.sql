-- Stored Procedure: sp_setup_workstations_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_workstations_upsert;

CREATE PROCEDURE sp_setup_workstations_upsert(
    IN p_workstation_id VARCHAR(32),
    IN p_building VARCHAR(64),
    IN p_workstation_name VARCHAR(64),
    IN p_modified_by_user_id BIGINT
)
INSERT INTO setup_workstations_catalog (
    id,
    public_id,
    building,
    workstation_name,
    is_active,
    sort_rank,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
VALUES (
    CAST(NULLIF(TRIM(p_workstation_id), '') AS UNSIGNED),
    UUID(),
    NULLIF(TRIM(p_building), ''),
    NULLIF(fn_setup_workstation_name_normalized(p_workstation_name), ''),
    1,
    100,
    p_modified_by_user_id,
    p_modified_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
)
ON DUPLICATE KEY UPDATE
    building = VALUES(building),
    workstation_name = VALUES(workstation_name),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();