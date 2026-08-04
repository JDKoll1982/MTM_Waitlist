-- Create procedure: sp_core_buildings_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_core_buildings_upsert;

DELIMITER $$

CREATE PROCEDURE sp_core_buildings_upsert(
    IN p_building_code VARCHAR(64),
    IN p_building_name VARCHAR(128),
    IN p_is_active TINYINT,
    IN p_updated_by_user_id BIGINT
)
BEGIN
    DECLARE v_building_id BIGINT DEFAULT NULL;
    DECLARE v_old_building_code VARCHAR(64);
    DECLARE v_old_building_name VARCHAR(128);
    DECLARE v_old_is_active TINYINT;

    IF p_building_code IS NULL OR TRIM(p_building_code) = '' OR p_building_name IS NULL OR TRIM(p_building_name) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Building code and name are required';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM auth_roles_assignments assignments
        INNER JOIN auth_roles_catalog roles ON roles.id = assignments.role_id
        WHERE assignments.user_id = p_updated_by_user_id
          AND roles.role_code IN ('admin', 'administrator', 'plant_manager', 'developer')
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Administrator role is required';
    END IF;

    SELECT id, building_code, building_name, is_active
    INTO v_building_id, v_old_building_code, v_old_building_name, v_old_is_active
    FROM core_buildings_catalog
    WHERE building_code = TRIM(p_building_code)
    LIMIT 1
    FOR UPDATE;

    IF v_building_id IS NULL THEN
        INSERT INTO core_buildings_catalog (
            public_id,
            building_code,
            building_name,
            is_active,
            created_utc,
            updated_utc,
            updated_by_user_id
        ) VALUES (
            UUID(),
            TRIM(p_building_code),
            TRIM(p_building_name),
            p_is_active,
            UTC_TIMESTAMP(),
            UTC_TIMESTAMP(),
            p_updated_by_user_id
        );
        SET v_building_id = LAST_INSERT_ID();
    ELSE
        INSERT INTO core_buildings_history (
            public_id,
            building_id,
            building_code,
            building_name,
            is_active,
            change_action,
            changed_by_user_id,
            changed_utc
        ) VALUES (
            UUID(),
            v_building_id,
            v_old_building_code,
            v_old_building_name,
            v_old_is_active,
            'update',
            p_updated_by_user_id,
            UTC_TIMESTAMP()
        );

        UPDATE core_buildings_catalog
        SET
            building_name = TRIM(p_building_name),
            is_active = p_is_active,
            updated_utc = UTC_TIMESTAMP(),
            updated_by_user_id = p_updated_by_user_id
        WHERE id = v_building_id;
    END IF;

    SELECT v_building_id AS building_id;
END$$

DELIMITER;