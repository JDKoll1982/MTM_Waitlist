-- Stored Procedure: sp_core_buildings_upsert
-- Engine: MySQL 5.7
--
-- Purpose: add a building, or rename one that exists.
--
-- Contract:
--   IN  p_original_name  VARCHAR(64)  the building being edited, or NULL/'' to add a new one
--   IN  p_building_name  VARCHAR(64)  the name the building is to carry
--   OUT the affected-row count, through the non-query seam.
--
-- Why a rename rewrites the rows that name the building
--   A work centre records `setup_work_centers_catalog.building` and a computer records
--   `core_computer_buildings.building_name`; both are plain names rather than identifiers. Renaming only the
--   catalog row would leave every work centre and every computer naming a building the catalog no longer holds,
--   which is a screen full of buildings that cannot be chosen. So the three writes are one transaction: the
--   rename either lands everywhere or nowhere.
--
-- Why a duplicate name is left to the store
--   `uq_core_buildings_catalog_building_name` is the authority on a name being taken, and it is enforced here
--   rather than by a read-then-write the application would have to race. A colliding name raises the store's
--   own duplicate-key error, which the caller reports as a refusal.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_buildings_upsert;

DELIMITER $$

CREATE PROCEDURE sp_core_buildings_upsert(
    IN p_original_name VARCHAR(64),
    IN p_building_name VARCHAR(64)
)
BEGIN
    DECLARE v_now_utc DATETIME;
    DECLARE v_next_rank INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_now_utc = fn_server_utc_now();

    START TRANSACTION;

    IF p_original_name IS NULL OR p_original_name = '' THEN
        SELECT COALESCE(MAX(sort_rank), 0) + 10
        INTO v_next_rank
        FROM core_buildings_catalog;

        INSERT INTO core_buildings_catalog (
            public_id,
            building_name,
            is_active,
            sort_rank,
            created_utc,
            updated_utc
        )
        VALUES (
            UUID(),
            p_building_name,
            1,
            v_next_rank,
            v_now_utc,
            v_now_utc
        );
    ELSE
        UPDATE core_buildings_catalog
        SET building_name = p_building_name,
            updated_utc = v_now_utc
        WHERE building_name = p_original_name;

        UPDATE setup_work_centers_catalog
        SET building = p_building_name,
            updated_utc = v_now_utc
        WHERE building = p_original_name;

        UPDATE core_computer_buildings
        SET building_name = p_building_name,
            updated_utc = v_now_utc
        WHERE building_name = p_original_name;
    END IF;

    COMMIT;
END$$

DELIMITER ;
