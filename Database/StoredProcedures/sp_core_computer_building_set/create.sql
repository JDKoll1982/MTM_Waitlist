-- Stored Procedure: sp_core_computer_building_set
-- Engine: MySQL 5.7
--
-- Purpose: record which building one computer stands in. Machine setup is the only caller, through
--          `MachineConfigurationService`, which is the single writer of a computer's configuration rows.
--
-- Contract:
--   IN  p_computer_name  VARCHAR(128)  a registry `computer_name` or its normalised hostname
--   OUT the affected-row count, through the non-query seam. A name that matches no registry row writes nothing,
--       which the caller reports as a configuration that was not written rather than as a success.
--
-- Why one row per computer is updated in place
--   `uq_core_computer_buildings_computer_id` is the identity here, not the row: a computer that is set up again
--   moves to the building it was asked for instead of gaining a second row, so nothing has to pick between two
--   answers later.
--
-- Why an unchanged answer reports zero rows
--   Setting a computer to the building it already stands in changes nothing, and the store reports that as zero
--   rows. The caller reads its answer back rather than counting rows, so a save that changed nothing is still a
--   save that succeeded.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_computer_building_set;

DELIMITER $$

CREATE PROCEDURE sp_core_computer_building_set(
    IN p_computer_name VARCHAR(128),
    IN p_building_name VARCHAR(64)
)
BEGIN
    DECLARE v_computer_id BIGINT DEFAULT NULL;
    DECLARE v_now_utc DATETIME;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_now_utc = fn_server_utc_now();

    SELECT r.id
    INTO v_computer_id
    FROM core_computers_registry r
    WHERE r.computer_name = p_computer_name
       OR r.hostname_normalized = p_computer_name
    ORDER BY r.id ASC
    LIMIT 1;

    IF v_computer_id IS NOT NULL THEN
        START TRANSACTION;

        IF EXISTS (SELECT 1 FROM core_computer_buildings WHERE computer_id = v_computer_id) THEN
            UPDATE core_computer_buildings
            SET building_name = p_building_name,
                updated_utc = v_now_utc
            WHERE computer_id = v_computer_id;
        ELSE
            INSERT INTO core_computer_buildings (
                public_id,
                computer_id,
                building_name,
                created_utc,
                updated_utc
            )
            VALUES (
                UUID(),
                v_computer_id,
                p_building_name,
                v_now_utc,
                v_now_utc
            );
        END IF;

        COMMIT;
    END IF;
END$$

DELIMITER ;
