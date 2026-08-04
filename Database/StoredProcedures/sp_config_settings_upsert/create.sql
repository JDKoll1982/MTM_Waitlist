-- Create procedure: sp_config_settings_upsert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_config_settings_upsert;

DELIMITER $$

CREATE PROCEDURE sp_config_settings_upsert(
    IN p_setting_key VARCHAR(190),
    IN p_scope_type VARCHAR(16),
    IN p_workstation_id BIGINT,
    IN p_user_id BIGINT,
    IN p_setting_value TEXT,
    IN p_setting_value_int BIGINT,
    IN p_setting_value_bool TINYINT,
    IN p_setting_value_decimal DECIMAL(18, 6),
    IN p_setting_value_datetime_utc DATETIME,
    IN p_value_type VARCHAR(32),
    IN p_updated_by_user_id BIGINT
)
BEGIN
    DECLARE v_scope_key VARCHAR(255);
    DECLARE v_setting_id BIGINT DEFAULT NULL;
    DECLARE v_old_setting_value TEXT;
    DECLARE v_old_setting_value_int BIGINT;
    DECLARE v_old_setting_value_bool TINYINT;
    DECLARE v_old_setting_value_decimal DECIMAL(18, 6);
    DECLARE v_old_setting_value_datetime_utc DATETIME;
    DECLARE v_old_value_type VARCHAR(32);
    DECLARE v_old_scope_type VARCHAR(16);
    DECLARE v_old_scope_key VARCHAR(255);
    DECLARE v_old_workstation_id BIGINT;
    DECLARE v_old_user_id BIGINT;

    IF p_setting_key IS NULL OR TRIM(p_setting_key) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'setting_key is required';
    END IF;

    IF p_scope_type IS NULL OR LOWER(TRIM(p_scope_type)) NOT IN ('workstation', 'all_users', 'user', 'admin', 'developer') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Unsupported settings scope';
    END IF;

    IF p_updated_by_user_id IS NULL OR NOT EXISTS (
        SELECT 1
        FROM core_users_profiles
        WHERE id = p_updated_by_user_id
          AND is_active = 1
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'An active updating user is required';
    END IF;

    IF LOWER(TRIM(p_scope_type)) = 'workstation' THEN
        IF p_workstation_id IS NULL OR NOT EXISTS (
            SELECT 1 FROM core_workstations_registry
            WHERE id = p_workstation_id AND is_registered = 1
        ) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A registered workstation is required';
        END IF;
        SET v_scope_key = CONCAT('workstation:', p_workstation_id);
    ELSEIF LOWER(TRIM(p_scope_type)) = 'user' THEN
        IF p_user_id IS NULL OR NOT EXISTS (
            SELECT 1 FROM core_users_profiles
            WHERE id = p_user_id AND is_active = 1
        ) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'An active user scope is required';
        END IF;
        SET v_scope_key = CONCAT('user:', p_user_id);
    ELSEIF LOWER(TRIM(p_scope_type)) = 'all_users' THEN
        SET v_scope_key = 'all_users';
    ELSEIF LOWER(TRIM(p_scope_type)) = 'admin' THEN
        SET v_scope_key = 'admin';
        IF NOT EXISTS (
            SELECT 1
            FROM auth_roles_assignments assignments
            INNER JOIN auth_roles_catalog roles ON roles.id = assignments.role_id
            WHERE assignments.user_id = p_updated_by_user_id
              AND roles.role_code IN ('admin', 'administrator', 'plant_manager', 'developer')
        ) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Administrator role is required';
        END IF;
    ELSE
        SET v_scope_key = 'developer';
        IF NOT EXISTS (
            SELECT 1
            FROM auth_roles_assignments assignments
            INNER JOIN auth_roles_catalog roles ON roles.id = assignments.role_id
            WHERE assignments.user_id = p_updated_by_user_id
              AND roles.role_code = 'developer'
        ) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Developer role is required';
        END IF;
    END IF;

    START TRANSACTION;

    SELECT
        id,
        setting_value,
        setting_value_int,
        setting_value_bool,
        setting_value_decimal,
        setting_value_datetime_utc,
        value_type,
        scope_type,
        scope_key,
        workstation_id,
        user_id
    INTO
        v_setting_id,
        v_old_setting_value,
        v_old_setting_value_int,
        v_old_setting_value_bool,
        v_old_setting_value_decimal,
        v_old_setting_value_datetime_utc,
        v_old_value_type,
        v_old_scope_type,
        v_old_scope_key,
        v_old_workstation_id,
        v_old_user_id
    FROM config_settings_values
    WHERE setting_key = TRIM(p_setting_key)
      AND scope_key = v_scope_key
    LIMIT 1
    FOR UPDATE;

    IF v_setting_id IS NULL THEN
        INSERT INTO config_settings_values (
            public_id,
            setting_key,
            scope_type,
            scope_key,
            workstation_id,
            user_id,
            setting_value,
            setting_value_int,
            setting_value_bool,
            setting_value_decimal,
            setting_value_datetime_utc,
            value_type,
            updated_by_user_id,
            updated_utc
        ) VALUES (
            UUID(),
            TRIM(p_setting_key),
            LOWER(TRIM(p_scope_type)),
            v_scope_key,
            p_workstation_id,
            p_user_id,
            p_setting_value,
            p_setting_value_int,
            p_setting_value_bool,
            p_setting_value_decimal,
            p_setting_value_datetime_utc,
            p_value_type,
            p_updated_by_user_id,
            UTC_TIMESTAMP()
        );

        SET v_setting_id = LAST_INSERT_ID();
    ELSE
        INSERT INTO config_settings_history (
            public_id,
            config_setting_id,
            setting_key,
            scope_type,
            scope_key,
            workstation_id,
            user_id,
            previous_setting_value,
            previous_setting_value_int,
            previous_setting_value_bool,
            previous_setting_value_decimal,
            previous_setting_value_datetime_utc,
            changed_setting_value,
            changed_setting_value_int,
            changed_setting_value_bool,
            changed_setting_value_decimal,
            changed_setting_value_datetime_utc,
            value_type,
            changed_by_user_id,
            changed_utc
        ) VALUES (
            UUID(),
            v_setting_id,
            TRIM(p_setting_key),
            v_old_scope_type,
            v_old_scope_key,
            v_old_workstation_id,
            v_old_user_id,
            v_old_setting_value,
            v_old_setting_value_int,
            v_old_setting_value_bool,
            v_old_setting_value_decimal,
            v_old_setting_value_datetime_utc,
            p_setting_value,
            p_setting_value_int,
            p_setting_value_bool,
            p_setting_value_decimal,
            p_setting_value_datetime_utc,
            p_value_type,
            p_updated_by_user_id,
            UTC_TIMESTAMP()
        );

        UPDATE config_settings_values
        SET
            setting_value = p_setting_value,
            setting_value_int = p_setting_value_int,
            setting_value_bool = p_setting_value_bool,
            setting_value_decimal = p_setting_value_decimal,
            setting_value_datetime_utc = p_setting_value_datetime_utc,
            value_type = p_value_type,
            updated_by_user_id = p_updated_by_user_id,
            updated_utc = UTC_TIMESTAMP()
        WHERE id = v_setting_id;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM config_settings_history
        WHERE config_setting_id = v_setting_id
          AND changed_by_user_id = p_updated_by_user_id
          AND changed_utc >= UTC_TIMESTAMP() - INTERVAL 1 SECOND
    ) THEN
        INSERT INTO config_settings_history (
            public_id,
            config_setting_id,
            setting_key,
            scope_type,
            scope_key,
            workstation_id,
            user_id,
            changed_setting_value,
            changed_setting_value_int,
            changed_setting_value_bool,
            changed_setting_value_decimal,
            changed_setting_value_datetime_utc,
            value_type,
            changed_by_user_id,
            changed_utc
        ) VALUES (
            UUID(),
            v_setting_id,
            TRIM(p_setting_key),
            LOWER(TRIM(p_scope_type)),
            v_scope_key,
            p_workstation_id,
            p_user_id,
            p_setting_value,
            p_setting_value_int,
            p_setting_value_bool,
            p_setting_value_decimal,
            p_setting_value_datetime_utc,
            p_value_type,
            p_updated_by_user_id,
            UTC_TIMESTAMP()
        );
    END IF;

    COMMIT;

    SELECT v_setting_id AS setting_id, v_scope_key AS scope_key;
END$$

DELIMITER;