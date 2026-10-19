SET FOREIGN_KEY_CHECKS=0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

DROP DATABASE IF EXISTS `mtm_receiving_application`;
CREATE DATABASE IF NOT EXISTS `mtm_receiving_application` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `mtm_receiving_application`;

DELIMITER $$
DROP PROCEDURE IF EXISTS `sp_Auth_Activity_Log`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_Activity_Log` (IN `p_event_type` VARCHAR(50), IN `p_username` VARCHAR(50), IN `p_workstation_name` VARCHAR(50), IN `p_details` TEXT)   BEGIN
    
    

    INSERT INTO settings_personal_activity_log (
        event_type,
        username,
        workstation_name,
        event_timestamp,
        details
    ) VALUES (
        p_event_type,
        p_username,
        p_workstation_name,
        NOW(),
        p_details
    );

    
    SELECT ROW_COUNT() AS rows_affected;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_Department_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_Department_GetAll` ()   BEGIN 

SELECT
    department_id,
    department_name,
    sort_order
FROM
    departments
WHERE
    is_active = TRUE
ORDER BY
    sort_order ASC,
    department_name ASC;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_Terminal_GetShared`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_Terminal_GetShared` ()   BEGIN 

SELECT
    workstation_name,
    workstation_type,
    description
FROM
    auth_workstation_config
WHERE
    workstation_type = 'shared_terminal'
    AND is_active = TRUE
ORDER BY
    workstation_name;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_Create`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_Create` (IN `p_employee_number` INT, IN `p_windows_username` VARCHAR(50), IN `p_full_name` VARCHAR(100), IN `p_pin` VARCHAR(4), IN `p_department` VARCHAR(50), IN `p_shift` VARCHAR(20), IN `p_created_by` VARCHAR(50), IN `p_visual_username` VARCHAR(50), IN `p_visual_password` VARCHAR(100), OUT `p_error_message` VARCHAR(500))   BEGIN DECLARE v_existing_count INT DEFAULT 0;

DECLARE v_emp_count INT DEFAULT 0;


SET
    p_error_message = NULL;


START TRANSACTION;


IF p_pin NOT REGEXP '^[0-9]{4}$' THEN
SET
    p_error_message = 'PIN must be exactly 4 numeric digits';

ROLLBACK;

END IF;


IF p_error_message IS NULL
AND (
    p_employee_number IS NULL
    OR p_employee_number <= 0
) THEN
SET
    p_error_message = 'Employee number must be a positive number';

ROLLBACK;

END IF;


IF p_error_message IS NULL THEN
SELECT
    COUNT(*) INTO v_emp_count
FROM
    auth_users
WHERE
    employee_number = p_employee_number;

IF v_emp_count > 0 THEN
SET
    p_error_message = 'Employee number already exists in database';

ROLLBACK;

END IF;

END IF;


IF p_error_message IS NULL THEN
SELECT
    COUNT(*) INTO v_existing_count
FROM
    auth_users
WHERE
    windows_username = p_windows_username;

IF v_existing_count > 0 THEN
SET
    p_error_message = 'Windows username already exists in database';

ROLLBACK;

END IF;

END IF;


IF p_error_message IS NULL
AND (
    p_full_name IS NULL
    OR TRIM(p_full_name) = ''
) THEN
SET
    p_error_message = 'Full Name is required';

ROLLBACK;

END IF;


IF p_error_message IS NULL
AND (
    p_department IS NULL
    OR TRIM(p_department) = ''
) THEN
SET
    p_error_message = 'Department is required';

ROLLBACK;

END IF;


IF p_error_message IS NULL
AND p_shift NOT IN ('1st Shift', '2nd Shift', '3rd Shift') THEN
SET
    p_error_message = 'Shift must be 1st Shift, 2nd Shift, or 3rd Shift';

ROLLBACK;

END IF;


IF p_error_message IS NULL THEN
INSERT INTO
    auth_users (
        employee_number,
        windows_username,
        full_name,
        pin,
        department,
        shift,
        is_active,
        visual_username,
        visual_password,
        created_by,
        created_date,
        modified_date
    )
VALUES
    (
        p_employee_number,
        p_windows_username,
        TRIM(p_full_name),
        p_pin,
        TRIM(p_department),
        p_shift,
        TRUE,
        NULLIF(TRIM(p_visual_username), ''),
        NULLIF(TRIM(p_visual_password), ''),
        p_created_by,
        NOW(),
        NOW()
    );




CALL sp_Auth_User_SeedDefaultModes(p_employee_number);


COMMIT;

END IF;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_Deactivate`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_Deactivate` (IN `p_employee_number` INT, IN `p_updated_by` VARCHAR(50), OUT `p_error_message` VARCHAR(500))   sp: BEGIN DECLARE v_existing_count INT DEFAULT 0;

SET
    p_error_message = NULL;


SELECT
    COUNT(*) INTO v_existing_count
FROM
    auth_users
WHERE
    employee_number = p_employee_number;

IF v_existing_count = 0 THEN
SET
    p_error_message = 'Employee not found';

LEAVE sp;

END IF;

START TRANSACTION;

UPDATE
    auth_users
SET
    is_active = 0,
    modified_date = NOW()
WHERE
    employee_number = p_employee_number;


INSERT INTO
    settings_personal_activity_log (
        event_type,
        username,
        workstation_name,
        event_timestamp,
        details
    )
VALUES
    (
        'user_deactivated',
        p_updated_by,
        'SETTINGS_UI',
        NOW(),
        CONCAT(
            'User deactivated: employee_number=',
            p_employee_number
        )
    );

COMMIT;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_GetAll` ()   BEGIN
SELECT
    employee_number,
    windows_username,
    full_name,
    pin,
    department,
    shift,
    is_active,
    visual_username,
    visual_password,
    default_receiving_mode,
    default_dunnage_mode,
    created_date,
    created_by,
    modified_date
FROM
    auth_users
ORDER BY
    full_name ASC;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_GetByWindowsUsername`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_GetByWindowsUsername` (IN `p_windows_username` VARCHAR(50))   BEGIN 

SELECT
    employee_number,
    windows_username,
    full_name,
    pin,
    department,
    shift,
    is_active,
    visual_username,
    visual_password,
    default_receiving_mode,
    default_dunnage_mode,
    created_date,
    created_by,
    modified_date
FROM
    auth_users
WHERE
    windows_username = p_windows_username
    AND is_active = TRUE
LIMIT
    1;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_GetDefaultMode`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_GetDefaultMode` (IN `p_user_id` INT)   BEGIN 
SELECT
    default_receiving_mode
FROM
    auth_users
WHERE
    employee_number = p_user_id;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_SeedDefaultModes`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_SeedDefaultModes` (IN `p_user_id` INT)   BEGIN
    DECLARE v_has_default_receiving_mode INT DEFAULT 0;
    DECLARE v_has_default_dunnage_mode INT DEFAULT 0;

    
    SELECT COUNT(*)
    INTO v_has_default_receiving_mode
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'auth_users'
      AND COLUMN_NAME = 'default_receiving_mode';

    IF v_has_default_receiving_mode > 0 THEN
        UPDATE auth_users
        SET default_receiving_mode = COALESCE(default_receiving_mode, 'guided'),
            modified_date = NOW()
        WHERE employee_number = p_user_id;
    END IF;

    
    SELECT COUNT(*)
    INTO v_has_default_dunnage_mode
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'auth_users'
      AND COLUMN_NAME = 'default_dunnage_mode';

    IF v_has_default_dunnage_mode > 0 THEN
        UPDATE auth_users
        SET default_dunnage_mode = COALESCE(default_dunnage_mode, 'guided'),
            modified_date = NOW()
        WHERE employee_number = p_user_id;
    END IF;

    SELECT 1 AS success;
END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_Update` (IN `p_employee_number` INT, IN `p_full_name` VARCHAR(100), IN `p_pin` VARCHAR(4), IN `p_department` VARCHAR(50), IN `p_shift` VARCHAR(20), IN `p_is_active` TINYINT(1), IN `p_visual_username` VARCHAR(50), IN `p_visual_password` VARCHAR(100), IN `p_updated_by` VARCHAR(50), OUT `p_error_message` VARCHAR(500))   sp: BEGIN DECLARE v_existing_count INT DEFAULT 0;

DECLARE v_workstation VARCHAR(50) DEFAULT 'SETTINGS_UI';

SET
    p_error_message = NULL;


SELECT
    COUNT(*) INTO v_existing_count
FROM
    auth_users
WHERE
    employee_number = p_employee_number;

IF v_existing_count = 0 THEN
SET
    p_error_message = 'Employee not found';

LEAVE sp;

END IF;


IF p_pin IS NOT NULL
AND p_pin NOT REGEXP '^[0-9]{4}$' THEN
SET
    p_error_message = 'PIN must be exactly 4 numeric digits';

LEAVE sp;

END IF;


IF p_shift NOT IN ('1st Shift', '2nd Shift', '3rd Shift') THEN
SET
    p_error_message = 'Shift must be 1st Shift, 2nd Shift, or 3rd Shift';

LEAVE sp;

END IF;

START TRANSACTION;

UPDATE
    auth_users
SET
    full_name = TRIM(p_full_name),
    pin = p_pin,
    department = TRIM(p_department),
    shift = p_shift,
    is_active = p_is_active,
    visual_username = NULLIF(TRIM(COALESCE(p_visual_username, '')), ''),
    visual_password = NULLIF(TRIM(COALESCE(p_visual_password, '')), ''),
    modified_date = NOW()
WHERE
    employee_number = p_employee_number;


INSERT INTO
    settings_personal_activity_log (
        event_type,
        username,
        workstation_name,
        event_timestamp,
        details
    )
VALUES
    (
        'user_updated',
        p_updated_by,
        v_workstation,
        NOW(),
        CONCAT(
            'User record updated for employee_number=',
            p_employee_number
        )
    );

COMMIT;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_UpdateDefaultDunnageMode`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_UpdateDefaultDunnageMode` (IN `p_user_id` INT, IN `p_default_mode` VARCHAR(20))   BEGIN
    UPDATE auth_users
    SET default_dunnage_mode = p_default_mode,
        modified_date = NOW()
    WHERE employee_number = p_user_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_UpdateDefaultMode`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_UpdateDefaultMode` (IN `p_user_id` INT, IN `p_default_mode` VARCHAR(20))   BEGIN
    
    

    UPDATE auth_users
    SET default_receiving_mode = p_default_mode
    WHERE employee_number = p_user_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_UpdateDefaultReceivingMode`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_UpdateDefaultReceivingMode` (IN `p_user_id` INT, IN `p_default_mode` VARCHAR(20))   BEGIN
    UPDATE auth_users
    SET default_receiving_mode = p_default_mode,
        modified_date = NOW()
    WHERE employee_number = p_user_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_UpdateVisualCredentials`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_UpdateVisualCredentials` (IN `p_employee_number` INT, IN `p_visual_username` VARCHAR(50), IN `p_visual_password` VARCHAR(100), IN `p_updated_by` VARCHAR(50), OUT `p_error_message` VARCHAR(500))   sp: BEGIN DECLARE v_existing_count INT DEFAULT 0;

SET
    p_error_message = NULL;


SELECT
    COUNT(*) INTO v_existing_count
FROM
    auth_users
WHERE
    employee_number = p_employee_number;

IF v_existing_count = 0 THEN
SET
    p_error_message = 'Employee not found';

LEAVE sp;

END IF;

START TRANSACTION;

UPDATE
    auth_users
SET
    visual_username = NULLIF(TRIM(COALESCE(p_visual_username, '')), ''),
    visual_password = NULLIF(TRIM(COALESCE(p_visual_password, '')), ''),
    modified_date = NOW()
WHERE
    employee_number = p_employee_number;


INSERT INTO
    settings_personal_activity_log (
        event_type,
        username,
        workstation_name,
        event_timestamp,
        details
    )
VALUES
    (
        'visual_credentials_updated',
        p_updated_by,
        'SETTINGS_UI',
        NOW(),
        CONCAT(
            'Visual credentials updated for employee_number=',
            p_employee_number
        )
    );

COMMIT;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_Upsert` (IN `p_employee_number` INT, IN `p_windows_username` VARCHAR(50), IN `p_full_name` VARCHAR(100), IN `p_pin` VARCHAR(4), IN `p_department` VARCHAR(50), IN `p_shift` VARCHAR(20), IN `p_is_active` BOOLEAN, IN `p_visual_username` VARCHAR(50), IN `p_visual_password` VARCHAR(100), IN `p_created_by` VARCHAR(50))   sp: BEGIN
    DECLARE v_emp_exists INT DEFAULT 0;
    DECLARE v_conflict_username INT DEFAULT 0;

    
    SELECT COUNT(*)
    INTO v_conflict_username
    FROM auth_users
    WHERE windows_username = p_windows_username
      AND employee_number <> p_employee_number;

    IF v_conflict_username > 0 THEN
        SELECT 0 AS success,
               0 AS affected_rows,
               'Windows username already assigned to a different employee_number' AS error_message;
        LEAVE sp;
    END IF;

    
    SELECT COUNT(*)
    INTO v_emp_exists
    FROM auth_users
    WHERE employee_number = p_employee_number;

    IF v_emp_exists > 0 THEN
        UPDATE auth_users
        SET windows_username = p_windows_username,
            full_name = p_full_name,
            pin = p_pin,
            department = p_department,
            shift = p_shift,
            is_active = p_is_active,
            visual_username = NULLIF(TRIM(p_visual_username), ''),
            visual_password = NULLIF(TRIM(p_visual_password), ''),
            created_by = p_created_by,
            modified_date = NOW()
        WHERE employee_number = p_employee_number;
    ELSE
        INSERT INTO auth_users (
            employee_number,
            windows_username,
            full_name,
            pin,
            department,
            shift,
            is_active,
            visual_username,
            visual_password,
            created_by,
            created_date,
            modified_date
        ) VALUES (
            p_employee_number,
            p_windows_username,
            p_full_name,
            p_pin,
            p_department,
            p_shift,
            p_is_active,
            NULLIF(TRIM(p_visual_username), ''),
            NULLIF(TRIM(p_visual_password), ''),
            p_created_by,
            NOW(),
            NOW()
        );
    END IF;

    
    CALL sp_Auth_User_SeedDefaultModes(p_employee_number);

    SELECT 1 AS success,
           ROW_COUNT() AS affected_rows,
           NULL AS error_message;
END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_ValidatePin`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_ValidatePin` (IN `p_username` VARCHAR(50), IN `p_pin` VARCHAR(4))   BEGIN
    
    

    SELECT
        employee_number,
        windows_username,
        full_name,
        pin,
        department,
        shift,
        is_active,
        visual_username,
        visual_password,
        default_receiving_mode,
        default_dunnage_mode,
        created_date,
        created_by,
        modified_date
    FROM auth_users
    WHERE (windows_username = p_username OR full_name = p_username)
      AND pin = p_pin
      AND is_active = TRUE
    LIMIT 1;

END$$

DROP PROCEDURE IF EXISTS `sp_Auth_Workstation_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_Workstation_Upsert` (IN `p_workstation_name` VARCHAR(50), IN `p_workstation_type` VARCHAR(50), IN `p_is_active` BOOLEAN, IN `p_description` VARCHAR(200))   BEGIN
    DECLARE v_type VARCHAR(50);

    SET v_type = LOWER(TRIM(COALESCE(p_workstation_type, '')));

    IF v_type NOT IN ('shared_terminal', 'personal_workstation') THEN
        SET v_type = 'personal_workstation';
    END IF;

    INSERT INTO auth_workstation_config (
        workstation_name,
        workstation_type,
        is_active,
        description,
        created_date,
        modified_date
    ) VALUES (
        TRIM(p_workstation_name),
        v_type,
        COALESCE(p_is_active, TRUE),
        NULLIF(TRIM(p_description), ''),
        NOW(),
        NOW()
    )
    ON DUPLICATE KEY UPDATE
        workstation_type = v_type,
        is_active = COALESCE(p_is_active, is_active),
        description = COALESCE(NULLIF(TRIM(p_description), ''), description),
        modified_date = NOW();
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_CustomFields_GetByType`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_CustomFields_GetByType` (IN `p_dunnage_type_id` INT)   BEGIN
    
    SELECT
        ID,
        DunnageTypeID,
        FieldName,
        DatabaseColumnName,
        FieldType,
        DisplayOrder,
        IsRequired,
        ValidationRules,
        CreatedDate,
        CreatedBy
    FROM dunnage_custom_fields
    WHERE DunnageTypeID = p_dunnage_type_id
    ORDER BY DisplayOrder;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_CustomFields_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_CustomFields_Insert` (IN `p_dunnage_type_id` INT, IN `p_field_name` VARCHAR(100), IN `p_database_column_name` VARCHAR(64), IN `p_field_type` VARCHAR(20), IN `p_display_order` INT, IN `p_is_required` BOOLEAN, IN `p_validation_rules` TEXT, IN `p_user` VARCHAR(50), OUT `p_new_id` INT, OUT `p_status` INT, OUT `p_error_msg` VARCHAR(500))   BEGIN
    
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1 p_error_msg = MESSAGE_TEXT;
        SET p_status = -1;
        SET p_new_id = NULL;
        ROLLBACK;
    END;

    
    START TRANSACTION;

    
    IF EXISTS (
        SELECT 1 FROM dunnage_custom_fields
        WHERE DunnageTypeID = p_dunnage_type_id
        AND DatabaseColumnName = p_database_column_name
    ) THEN
        SET p_status = -1;
        SET p_error_msg = CONCAT('Column name "', p_database_column_name, '" already exists for this type');
        SET p_new_id = NULL;
        ROLLBACK;
    ELSE
        
        INSERT INTO dunnage_custom_fields (
            DunnageTypeID,
            FieldName,
            DatabaseColumnName,
            FieldType,
            DisplayOrder,
            IsRequired,
            ValidationRules,
            CreatedDate,
            CreatedBy
        ) VALUES (
            p_dunnage_type_id,
            p_field_name,
            p_database_column_name,
            p_field_type,
            p_display_order,
            p_is_required,
            p_validation_rules,
            NOW(),
            p_user
        );

        
        SET p_new_id = LAST_INSERT_ID();
        SET p_status = 1;
        SET p_error_msg = 'Custom field created successfully';
        COMMIT;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Inventory_Check`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Inventory_Check` (IN `p_part_id` VARCHAR(50))   BEGIN
    SELECT COUNT(*) > 0 as requires_inventory
    FROM dunnage_requires_inventory
    WHERE part_id = p_part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Inventory_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Inventory_Delete` (IN `p_id` INT)   BEGIN
    DELETE FROM dunnage_requires_inventory
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Inventory_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Inventory_GetAll` ()   BEGIN
    SELECT
        i.id,
        i.part_id,
        t.type_name,
        i.inventory_method,
        i.notes,
        i.created_by,
        i.created_date,
        i.modified_by,
        i.modified_date
    FROM dunnage_requires_inventory i
    JOIN dunnage_parts p ON i.part_id = p.part_id
    JOIN dunnage_types t ON p.type_id = t.id
    ORDER BY i.part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Inventory_GetByPart`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Inventory_GetByPart` (IN `p_part_id` VARCHAR(50))   BEGIN
    SELECT
        i.id,
        i.part_id,
        t.type_name,
        i.inventory_method,
        i.notes,
        i.created_by,
        i.created_date,
        i.modified_by,
        i.modified_date
    FROM dunnage_requires_inventory i
    JOIN dunnage_parts p ON i.part_id = p.part_id
    JOIN dunnage_types t ON p.type_id = t.id
    WHERE i.part_id = p_part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Inventory_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Inventory_Insert` (IN `p_part_id` VARCHAR(50), IN `p_inventory_method` VARCHAR(100), IN `p_notes` TEXT, IN `p_user` VARCHAR(50), OUT `p_new_id` INT)   BEGIN
    INSERT INTO dunnage_requires_inventory (
        part_id,
        inventory_method,
        notes,
        created_by,
        created_date
    ) VALUES (
        p_part_id,
        p_inventory_method,
        p_notes,
        p_user,
        NOW()
    );

    SET p_new_id = LAST_INSERT_ID();
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Inventory_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Inventory_Update` (IN `p_id` INT, IN `p_inventory_method` VARCHAR(100), IN `p_notes` TEXT, IN `p_user` VARCHAR(50))   BEGIN
    UPDATE dunnage_requires_inventory
    SET
        inventory_method = p_inventory_method,
        notes = p_notes,
        modified_by = p_user,
        modified_date = NOW()
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_LabelData_ClearToHistory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_LabelData_ClearToHistory` (IN `p_archived_by` VARCHAR(100), OUT `p_rows_moved` INT, OUT `p_archive_batch_id` CHAR(36), OUT `p_status` INT, OUT `p_error_message` VARCHAR(1000))   BEGIN
    DECLARE v_rows_to_move INT DEFAULT 0;

    
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        SET p_rows_moved       = 0;
        SET p_status           = 1;
        SET p_error_message    = 'Clear Label Data failed. Transaction rolled back.';
    END;

    SET p_rows_moved      = 0;
    SET p_status          = 0;
    SET p_error_message   = NULL;
    SET p_archive_batch_id = UUID();

    START TRANSACTION;

    SELECT COUNT(*) INTO v_rows_to_move
    FROM dunnage_label_data;

    IF v_rows_to_move = 0 THEN
        
        COMMIT;
    ELSE
        
        INSERT INTO dunnage_history
        (
            load_uuid,
            part_id,
            quantity,
            received_date,
            created_by,
            created_date,
            po_number,
            type_id,
            type_name,
            type_icon,
            location,
            label_number,
            specs_json,
            archived_at,
            archived_by,
            archive_batch_id
        )
        SELECT
            dld.load_uuid,
            dld.part_id,
            dld.quantity,
            COALESCE(dld.received_date, NOW())  AS received_date,
            dld.user_id                         AS created_by,
            NOW()                               AS created_date,
            dld.po_number,
            dld.dunnage_type_id                 AS type_id,
            dld.dunnage_type_name               AS type_name,
            dld.dunnage_type_icon               AS type_icon,
            dld.location,
            dld.label_number,
            dld.specs_json,
            NOW()                               AS archived_at,
            p_archived_by                       AS archived_by,
            p_archive_batch_id                  AS archive_batch_id
        FROM dunnage_label_data dld;

        
        DELETE FROM dunnage_label_data;

        COMMIT;

        SET p_rows_moved = v_rows_to_move;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_LabelData_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_LabelData_GetAll` ()   BEGIN
    SELECT
        load_uuid,
        part_id,
        dunnage_type_id,
        dunnage_type_name,
        dunnage_type_icon,
        quantity,
        po_number,
        received_date,
        user_id,
        location,
        label_number,
        specs_json
    FROM dunnage_label_data
    ORDER BY received_date ASC;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_LabelData_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_LabelData_Insert` (IN `p_load_uuid` CHAR(36), IN `p_part_id` VARCHAR(50), IN `p_dunnage_type_id` INT, IN `p_dunnage_type_name` VARCHAR(100), IN `p_dunnage_type_icon` VARCHAR(100), IN `p_quantity` DECIMAL(10,2), IN `p_po_number` VARCHAR(50), IN `p_received_date` DATETIME, IN `p_user_id` VARCHAR(100), IN `p_location` VARCHAR(100), IN `p_label_number` VARCHAR(50), IN `p_specs_json` JSON)   BEGIN
    INSERT INTO dunnage_label_data
    (
        load_uuid,
        part_id,
        dunnage_type_id,
        dunnage_type_name,
        dunnage_type_icon,
        quantity,
        po_number,
        received_date,
        user_id,
        location,
        label_number,
        specs_json
    )
    VALUES
    (
        p_load_uuid,
        p_part_id,
        p_dunnage_type_id,
        p_dunnage_type_name,
        p_dunnage_type_icon,
        p_quantity,
        p_po_number,
        COALESCE(p_received_date, NOW()),
        p_user_id,
        p_location,
        p_label_number,
        p_specs_json
    );
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_LabelData_InsertBatch`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_LabelData_InsertBatch` (IN `p_load_data` JSON, IN `p_user` VARCHAR(100))   BEGIN
    DECLARE i     INT DEFAULT 0;
    DECLARE cnt   INT DEFAULT 0;
    DECLARE v_load_uuid         CHAR(36);
    DECLARE v_part_id           VARCHAR(50);
    DECLARE v_dunnage_type_id   INT;
    DECLARE v_dunnage_type_name VARCHAR(100);
    DECLARE v_dunnage_type_icon VARCHAR(100);
    DECLARE v_quantity          DECIMAL(10,2);
    DECLARE v_po_number         VARCHAR(50);
    DECLARE v_received_date     DATETIME;
    DECLARE v_location          VARCHAR(100);
    DECLARE v_label_number      VARCHAR(50);
    DECLARE v_specs_json        JSON;

    SET cnt = JSON_LENGTH(p_load_data);

    WHILE i < cnt DO
        SET v_load_uuid         = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].load_uuid')));
        SET v_part_id           = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].part_id')));
        SET v_dunnage_type_id   = JSON_EXTRACT(p_load_data,             CONCAT('$[', i, '].dunnage_type_id'));
        SET v_dunnage_type_name = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].dunnage_type_name')));
        SET v_dunnage_type_icon = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].dunnage_type_icon')));
        SET v_quantity          = JSON_EXTRACT(p_load_data,             CONCAT('$[', i, '].quantity'));
        SET v_po_number         = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].po_number')));
        SET v_received_date     = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].received_date')));
        SET v_location          = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].location')));
        SET v_label_number      = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].label_number')));
        SET v_specs_json        = JSON_EXTRACT(p_load_data,             CONCAT('$[', i, '].specs_json'));

        INSERT INTO dunnage_label_data
        (
            load_uuid,
            part_id,
            dunnage_type_id,
            dunnage_type_name,
            dunnage_type_icon,
            quantity,
            po_number,
            received_date,
            user_id,
            location,
            label_number,
            specs_json
        )
        VALUES
        (
            v_load_uuid,
            v_part_id,
            v_dunnage_type_id,
            v_dunnage_type_name,
            v_dunnage_type_icon,
            v_quantity,
            NULLIF(v_po_number, 'null'),
            COALESCE(v_received_date, NOW()),
            p_user,
            NULLIF(v_location, 'null'),
            NULLIF(v_label_number, 'null'),
            v_specs_json
        );

        SET i = i + 1;
    END WHILE;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_Delete` (IN `p_load_uuid` CHAR(36))   BEGIN
    DELETE FROM dunnage_history
    WHERE load_uuid = p_load_uuid;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_GetAll` ()   BEGIN
    SELECT
        l.load_uuid,
        l.part_id,
        p.type_id,
        t.type_name,
        l.quantity,
        l.received_date,
        l.created_by,
        l.created_date,
        l.modified_by,
        l.modified_date
    FROM dunnage_history l
    JOIN dunnage_parts p ON l.part_id = p.part_id
    JOIN dunnage_types t ON p.type_id = t.id
    ORDER BY l.received_date DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_GetByDateRange`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_GetByDateRange` (IN `p_start_date` DATETIME, IN `p_end_date` DATETIME)   BEGIN
    SELECT
        l.load_uuid,
        l.part_id,
        p.type_id,
        t.type_name,
        l.quantity,
        l.received_date,
        l.created_by,
        l.created_date,
        l.modified_by,
        l.modified_date
    FROM dunnage_history l
    JOIN dunnage_parts p ON l.part_id = p.part_id
    JOIN dunnage_types t ON p.type_id = t.id
    WHERE l.received_date BETWEEN p_start_date AND p_end_date
    ORDER BY l.received_date DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_GetById` (IN `p_load_uuid` CHAR(36))   BEGIN
    SELECT
        l.load_uuid,
        l.part_id,
        p.type_id,
        t.type_name,
        l.quantity,
        l.received_date,
        l.created_by,
        l.created_date,
        l.modified_by,
        l.modified_date
    FROM dunnage_history l
    JOIN dunnage_parts p ON l.part_id = p.part_id
    JOIN dunnage_types t ON p.type_id = t.id
    WHERE l.load_uuid = p_load_uuid;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_Update` (IN `p_load_uuid` CHAR(36), IN `p_quantity` DECIMAL(10,2), IN `p_user` VARCHAR(50))   BEGIN
    UPDATE dunnage_history
    SET
        quantity = p_quantity,
        modified_by = p_user,
        modified_date = NOW()
    WHERE load_uuid = p_load_uuid;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_NonPO_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_NonPO_Delete` (IN `p_id` INT UNSIGNED)   BEGIN
    DELETE FROM `dunnage_non_po_entries` WHERE `id` = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_NonPO_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_NonPO_GetAll` ()   BEGIN
    SELECT
        `id`,
        `value`,
        `created_by`,
        `created_at`,
        `use_count`
    FROM   `dunnage_non_po_entries`
    ORDER BY `use_count` DESC, `created_at` DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_NonPO_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_NonPO_Upsert` (IN `p_value` VARCHAR(100), IN `p_created_by` VARCHAR(100))   BEGIN
    INSERT INTO `dunnage_non_po_entries` (`value`, `created_by`, `use_count`)
    VALUES (p_value, p_created_by, 1)
    ON DUPLICATE KEY UPDATE `use_count` = `use_count` + 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_CountTransactions`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_CountTransactions` (IN `p_part_id` VARCHAR(50))   BEGIN
    SELECT COUNT(*) as transaction_count
    FROM dunnage_history
    WHERE part_id = p_part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_Delete` (IN `p_id` INT)   BEGIN
    DELETE FROM dunnage_parts
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_GetAll` ()   BEGIN
    SELECT
        p.id,
        p.part_id,
        p.type_id,
        t.type_name,
        p.spec_values,
        p.home_location,
        p.created_by,
        p.created_date,
        p.modified_by,
        p.modified_date
    FROM dunnage_parts p
    JOIN dunnage_types t ON p.type_id = t.id
    ORDER BY p.part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_GetById` (IN `p_part_id` VARCHAR(50))   BEGIN
    SELECT
        p.id,
        p.part_id,
        p.type_id,
        t.type_name,
        p.spec_values,
        p.home_location,
        p.created_by,
        p.created_date,
        p.modified_by,
        p.modified_date
    FROM dunnage_parts p
    JOIN dunnage_types t ON p.type_id = t.id
    WHERE p.part_id = p_part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_GetByType`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_GetByType` (IN `p_type_id` INT)   BEGIN
    SELECT
        p.id,
        p.part_id,
        p.type_id,
        t.type_name,
        p.spec_values,
        p.home_location,
        p.created_by,
        p.created_date,
        p.modified_by,
        p.modified_date
    FROM dunnage_parts p
    JOIN dunnage_types t ON p.type_id = t.id
    WHERE p.type_id = p_type_id
    ORDER BY p.part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_GetTransactionCount`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_GetTransactionCount` (IN `p_part_id` VARCHAR(100), OUT `p_count` INT)   BEGIN
    
    SELECT COUNT(*) INTO p_count
    FROM dunnage_history
    WHERE part_id = p_part_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_Insert` (IN `p_part_id` VARCHAR(50), IN `p_type_id` INT, IN `p_spec_values` JSON, IN `p_home_location` VARCHAR(100), IN `p_user` VARCHAR(50), OUT `p_new_id` INT)   BEGIN
    INSERT INTO dunnage_parts (
        part_id,
        type_id,
        spec_values,
        home_location,
        created_by,
        created_date
    ) VALUES (
        p_part_id,
        p_type_id,
        p_spec_values,
        p_home_location,
        p_user,
        NOW()
    );
    
    SET p_new_id = LAST_INSERT_ID();
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_Search`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_Search` (IN `p_search_text` VARCHAR(100), IN `p_type_id` INT)   BEGIN
    
    
    
    

    SELECT
        p.id,
        p.part_id,
        p.type_id,
        t.type_name,
        p.spec_values,
        p.created_by,
        p.created_date,
        p.modified_by,
        p.modified_date
    FROM dunnage_parts p
    JOIN dunnage_types t ON p.type_id = t.id
    WHERE (p_type_id IS NULL OR p_type_id = 0 OR p.type_id = p_type_id)
    AND (
        LOWER(p.part_id) LIKE LOWER(CONCAT('%', p_search_text, '%'))
        OR JSON_SEARCH(LOWER(p.spec_values), 'one', LOWER(CONCAT('%', p_search_text, '%'))) IS NOT NULL
    )
    ORDER BY p.part_id
    LIMIT 100;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Parts_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Parts_Update` (IN `p_id` INT, IN `p_spec_values` JSON, IN `p_home_location` VARCHAR(100), IN `p_user` VARCHAR(50))   BEGIN
    UPDATE dunnage_parts
    SET 
        spec_values = p_spec_values,
        home_location = p_home_location,
        modified_by = p_user,
        modified_date = NOW()
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_CountPartsUsingSpec`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_CountPartsUsingSpec` (IN `p_type_id` INT, IN `p_spec_key` VARCHAR(100))   BEGIN
    
    
    SELECT COUNT(*) as part_count
    FROM dunnage_parts
    WHERE type_id = p_type_id
    AND JSON_CONTAINS_PATH(spec_values, 'one', CONCAT('$.', p_spec_key)) = 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_DeleteById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_DeleteById` (IN `p_id` INT)   BEGIN
    DELETE FROM dunnage_specs
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_DeleteByType`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_DeleteByType` (IN `p_type_id` INT)   BEGIN
    DELETE FROM dunnage_specs
    WHERE type_id = p_type_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_GetAll` ()   BEGIN
    SELECT 
        id,
        type_id,
        spec_key,
        spec_value,
        created_by,
        created_date,
        modified_by,
        modified_date
    FROM dunnage_specs;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_GetAllKeys`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_GetAllKeys` ()   BEGIN
    CREATE TEMPORARY TABLE IF NOT EXISTS temp_spec_keys (
        SpecKey VARCHAR(255)
    );

    TRUNCATE TABLE temp_spec_keys;

    INSERT INTO temp_spec_keys (SpecKey)
    SELECT DISTINCT
        JSON_UNQUOTE(JSON_EXTRACT(JSON_KEYS(ds.spec_value), CONCAT('$[', nums.n - 1, ']'))) AS SpecKey
    FROM dunnage_specs ds
    JOIN (
        SELECT  1 n UNION ALL SELECT  2 UNION ALL SELECT  3 UNION ALL SELECT  4 UNION ALL SELECT  5
        UNION ALL SELECT  6 UNION ALL SELECT  7 UNION ALL SELECT  8 UNION ALL SELECT  9 UNION ALL SELECT 10
        UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
        UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
        UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL SELECT 25
        UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30
        UNION ALL SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 33 UNION ALL SELECT 34 UNION ALL SELECT 35
        UNION ALL SELECT 36 UNION ALL SELECT 37 UNION ALL SELECT 38 UNION ALL SELECT 39 UNION ALL SELECT 40
        UNION ALL SELECT 41 UNION ALL SELECT 42 UNION ALL SELECT 43 UNION ALL SELECT 44 UNION ALL SELECT 45
        UNION ALL SELECT 46 UNION ALL SELECT 47 UNION ALL SELECT 48 UNION ALL SELECT 49 UNION ALL SELECT 50
        UNION ALL SELECT 51 UNION ALL SELECT 52 UNION ALL SELECT 53 UNION ALL SELECT 54 UNION ALL SELECT 55
        UNION ALL SELECT 56 UNION ALL SELECT 57 UNION ALL SELECT 58 UNION ALL SELECT 59 UNION ALL SELECT 60
        UNION ALL SELECT 61 UNION ALL SELECT 62 UNION ALL SELECT 63 UNION ALL SELECT 64 UNION ALL SELECT 65
        UNION ALL SELECT 66 UNION ALL SELECT 67 UNION ALL SELECT 68 UNION ALL SELECT 69 UNION ALL SELECT 70
        UNION ALL SELECT 71 UNION ALL SELECT 72 UNION ALL SELECT 73 UNION ALL SELECT 74 UNION ALL SELECT 75
        UNION ALL SELECT 76 UNION ALL SELECT 77 UNION ALL SELECT 78 UNION ALL SELECT 79 UNION ALL SELECT 80
        UNION ALL SELECT 81 UNION ALL SELECT 82 UNION ALL SELECT 83 UNION ALL SELECT 84 UNION ALL SELECT 85
        UNION ALL SELECT 86 UNION ALL SELECT 87 UNION ALL SELECT 88 UNION ALL SELECT 89 UNION ALL SELECT 90
        UNION ALL SELECT 91 UNION ALL SELECT 92 UNION ALL SELECT 93 UNION ALL SELECT 94 UNION ALL SELECT 95
        UNION ALL SELECT 96 UNION ALL SELECT 97 UNION ALL SELECT 98 UNION ALL SELECT 99 UNION ALL SELECT 100
    ) AS nums
    WHERE ds.spec_value IS NOT NULL
      AND JSON_TYPE(ds.spec_value) = 'OBJECT'
      AND nums.n <= JSON_LENGTH(JSON_KEYS(ds.spec_value));

    SELECT DISTINCT SpecKey
    FROM temp_spec_keys
    WHERE SpecKey IS NOT NULL AND SpecKey != ''
    ORDER BY SpecKey;

    DROP TEMPORARY TABLE IF EXISTS temp_spec_keys;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_GetById` (IN `p_id` INT)   BEGIN
    SELECT 
        id,
        type_id,
        spec_key,
        spec_value,
        created_by,
        created_date,
        modified_by,
        modified_date
    FROM dunnage_specs
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_GetByType`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_GetByType` (IN `p_type_id` INT)   BEGIN
    SELECT 
        id,
        type_id,
        spec_key,
        spec_value,
        created_by,
        created_date,
        modified_by,
        modified_date
    FROM dunnage_specs
    WHERE type_id = p_type_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_Insert` (IN `p_type_id` INT, IN `p_spec_key` VARCHAR(100), IN `p_spec_value` JSON, IN `p_user` VARCHAR(50), OUT `p_new_id` INT)   BEGIN
    INSERT INTO dunnage_specs (
        type_id,
        spec_key,
        spec_value,
        created_by,
        created_date
    ) VALUES (
        p_type_id,
        p_spec_key,
        p_spec_value,
        p_user,
        NOW()
    );
    
    SET p_new_id = LAST_INSERT_ID();
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Specs_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Specs_Update` (IN `p_id` INT, IN `p_spec_value` JSON, IN `p_user` VARCHAR(50))   BEGIN
    UPDATE dunnage_specs
    SET 
        spec_value = p_spec_value,
        modified_by = p_user,
        modified_date = NOW()
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_CheckDuplicate`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_CheckDuplicate` (IN `p_type_name` VARCHAR(100), IN `p_exclude_id` INT, OUT `p_exists` BOOLEAN)   BEGIN
    
    IF EXISTS (
        SELECT 1 FROM dunnage_types
        WHERE type_name = p_type_name
        AND (p_exclude_id IS NULL OR id != p_exclude_id)
    ) THEN
        SET p_exists = TRUE;
    ELSE
        SET p_exists = FALSE;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_CountParts`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_CountParts` (IN `p_type_id` INT)   BEGIN
    SELECT COUNT(*) as part_count
    FROM dunnage_parts
    WHERE type_id = p_type_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_CountTransactions`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_CountTransactions` (IN `p_type_id` INT)   BEGIN
    SELECT COUNT(*) as transaction_count
    FROM dunnage_history l
    JOIN dunnage_parts p ON l.part_id = p.part_id
    WHERE p.type_id = p_type_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_Delete` (IN `p_id` INT, IN `p_modified_by` VARCHAR(50), OUT `p_status` INT, OUT `p_error_msg` VARCHAR(500))   BEGIN
    DECLARE v_exists INT DEFAULT 0;
    DECLARE v_parts_count INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1 p_error_msg = MESSAGE_TEXT;
        SET p_status = -1;
        ROLLBACK;
    END;

    START TRANSACTION;

    
    SELECT COUNT(*) INTO v_exists
    FROM dunnage_types
    WHERE id = p_id;

    IF v_exists = 0 THEN
        SET p_status = 0;
        SET p_error_msg = 'Dunnage type not found';
        ROLLBACK;
    ELSE
        
        SELECT COUNT(*) INTO v_parts_count
        FROM dunnage_parts
        WHERE dunnage_type_id = p_id;

        IF v_parts_count > 0 THEN
            SET p_status = -1;
            SET p_error_msg = CONCAT('Cannot delete dunnage type that is in use by ', v_parts_count, ' part(s)');
            ROLLBACK;
        ELSE
            
            DELETE FROM dunnage_types
            WHERE id = p_id;

            SET p_status = 1;
            SET p_error_msg = 'Dunnage type deleted successfully';
            COMMIT;
        END IF;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_GetAll` ()   BEGIN
    SELECT
        *
    FROM dunnage_types
    ORDER BY id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_GetById` (IN `p_id` INT)   BEGIN
    SELECT
        *
    FROM dunnage_types
    WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_GetPartCount`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_GetPartCount` (IN `p_type_id` INT, OUT `p_count` INT)   BEGIN
    
    SELECT COUNT(*) INTO p_count
    FROM dunnage_parts
    WHERE type_id = p_type_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_GetTransactionCount`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_GetTransactionCount` (IN `p_type_id` INT, OUT `p_count` INT)   BEGIN
    
    
    
    SELECT COUNT(*) INTO p_count
    FROM dunnage_history dh
    INNER JOIN dunnage_parts dp ON dh.part_id = dp.part_id
    WHERE dp.type_id = p_type_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_GetUsageCount`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_GetUsageCount` (IN `p_id` INT)   BEGIN
    SELECT COUNT(*) AS usage_count
    FROM receiving_package_type_mapping
    WHERE package_type = (SELECT type_name FROM dunnage_types WHERE id = p_id)
      AND is_active = TRUE;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_Insert` (IN `p_type_name` VARCHAR(100), IN `p_icon` VARCHAR(50), IN `p_user` VARCHAR(50), OUT `p_new_id` INT)   BEGIN
    
    IF EXISTS (SELECT 1 FROM dunnage_types WHERE type_name = p_type_name) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Dunnage type name already exists';
    END IF;

    INSERT INTO dunnage_types (type_name, icon, created_by, created_date)
    VALUES (p_type_name, p_icon, p_user, NOW());

    SET p_new_id = LAST_INSERT_ID();
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Types_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_Update` (IN `p_id` INT, IN `p_type_name` VARCHAR(100), IN `p_icon` VARCHAR(50), IN `p_modified_by` VARCHAR(50))   BEGIN
    
    IF EXISTS (SELECT 1 FROM dunnage_types WHERE type_name = p_type_name AND id != p_id) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Dunnage type name already exists';
    END IF;

    UPDATE dunnage_types
    SET type_name = p_type_name,
        icon = p_icon,
        modified_by = p_modified_by,
        modified_date = CURRENT_TIMESTAMP
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_UserPreferences_GetRecentIcons`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_UserPreferences_GetRecentIcons` (IN `p_user_id` VARCHAR(50), IN `p_count` INT)   BEGIN
    SELECT
        SUBSTRING(PreferenceKey, 12) as icon_name
    FROM settings_dunnage_personal
        WHERE UserId = (CONVERT(p_user_id USING utf8mb4) COLLATE utf8mb4_general_ci)
      AND PreferenceKey LIKE 'RecentIcon_%'
    ORDER BY LastUpdated DESC
    LIMIT p_count;
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_UserPreferences_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_UserPreferences_Upsert` (IN `p_user_id` VARCHAR(50), IN `p_pref_key` VARCHAR(100), IN `p_pref_value` TEXT)   BEGIN
    INSERT INTO settings_dunnage_personal (UserId, PreferenceKey, PreferenceValue, LastUpdated)
    VALUES (p_user_id, p_pref_key, p_pref_value, NOW())
    ON DUPLICATE KEY UPDATE
        PreferenceValue = VALUES(PreferenceValue),
        LastUpdated     = NOW();
END$$

DROP PROCEDURE IF EXISTS `sp_receiving_dunnage_types_get_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_receiving_dunnage_types_get_all` ()   SELECT id, type_name
FROM dunnage_types
WHERE type_name IS NOT NULL
    AND TRIM(type_name) <> ''
ORDER BY type_name ASC$$

DROP PROCEDURE IF EXISTS `sp_receiving_history_average_coil_weight`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_receiving_history_average_coil_weight` (IN `p_part_id` VARCHAR(64))   SELECT ROUND(AVG(quantity), 0) AS AverageWeight
FROM receiving_history
WHERE part_id = TRIM(p_part_id)
  AND quantity IS NOT NULL
  AND quantity > 0$$

DROP PROCEDURE IF EXISTS `sp_Receiving_History_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_History_Get` (IN `p_PartID` VARCHAR(50), IN `p_StartDate` DATE, IN `p_EndDate` DATE)   BEGIN
    SELECT
        load_guid           AS LoadID,
        part_id             AS PartID,
        part_description    AS PartDescription,
        NULL                AS PartType,
        po_number           AS PONumber,
        NULL                AS POLineNumber,
        vendor_name         AS POVendor,
        NULL                AS POStatus,
        NULL                AS PODueDate,
        NULL                AS QtyOrdered,
        NULL                AS UnitOfMeasure,
        NULL                AS RemainingQuantity,
        label_number        AS LoadNumber,
        quantity            AS WeightQuantity,
        heat                AS HeatLotNumber,
        NULL                AS PackagesPerLoad,
        NULL                AS PackageTypeName,
        NULL                AS WeightPerPackage,
        is_non_po_item      AS IsNonPOItem,
        transaction_date    AS ReceivedDate,
        NULL                AS UserID,
        employee_number     AS EmployeeNumber,
        0                   AS IsQualityHoldRequired,
        0                   AS IsQualityHoldAcknowledged,
        NULL                AS QualityHoldRestrictionType
    FROM receiving_history
    WHERE
        (p_PartID    IS NULL OR part_id          = p_PartID)
        AND (p_StartDate IS NULL OR transaction_date >= p_StartDate)
        AND (p_EndDate   IS NULL OR transaction_date <= p_EndDate)
    ORDER BY transaction_date DESC, id DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_History_Import`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_History_Import` (IN `p_Quantity` INT, IN `p_PartID` VARCHAR(50), IN `p_PONumber` VARCHAR(20), IN `p_EmployeeNumber` INT, IN `p_Heat` VARCHAR(100), IN `p_TransactionDate` DATE, IN `p_InitialLocation` VARCHAR(50), IN `p_CoilsOnSkid` INT, IN `p_LabelNumber` INT, IN `p_VendorName` VARCHAR(255), IN `p_PartDescription` VARCHAR(500), IN `p_IsNonPOItem` TINYINT(1))   BEGIN
    INSERT INTO receiving_history
    (
        quantity,
        part_id,
        po_number,
        employee_number,
        heat,
        transaction_date,
        initial_location,
        coils_on_skid,
        label_number,
        vendor_name,
        part_description,
        is_non_po_item
    )
    VALUES
    (
        p_Quantity,
        p_PartID,
        NULLIF(TRIM(p_PONumber), ''),
        p_EmployeeNumber,
        NULLIF(TRIM(p_Heat), ''),
        p_TransactionDate,
        NULLIF(TRIM(p_InitialLocation), ''),
        p_CoilsOnSkid,
        IFNULL(p_LabelNumber, 1),
        NULLIF(TRIM(p_VendorName), ''),
        NULLIF(TRIM(p_PartDescription), ''),
        IFNULL(p_IsNonPOItem, 0)
    );

    
    
    SELECT LAST_INSERT_ID() INTO @last_import_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_LabelData_ClearToHistory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_LabelData_ClearToHistory` (IN `p_archived_by` VARCHAR(100), OUT `p_rows_moved` INT, OUT `p_archive_batch_id` CHAR(36), OUT `p_status` INT, OUT `p_error_message` VARCHAR(1000))   BEGIN
    DECLARE v_rows_to_move INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        SET p_rows_moved = 0;
        SET p_status = 1;
        SET p_error_message = 'Clear Label Data failed. Transaction rolled back.';
    END;

    SET p_rows_moved = 0;
    SET p_status = 0;
    SET p_error_message = NULL;
    SET p_archive_batch_id = UUID();

    START TRANSACTION;

    SELECT COUNT(*) INTO v_rows_to_move
    FROM receiving_label_data;

    IF v_rows_to_move = 0 THEN
        COMMIT;
        SET p_rows_moved = 0;
        SET p_status = 0;
        SET p_error_message = NULL;
    ELSE
        
        
        
        
        INSERT INTO receiving_history
        (
            load_guid,
            quantity,
            part_id,
            part_description,
            po_number,
            employee_number,
            heat,
            transaction_date,
            initial_location,
            coils_on_skid,
            label_number,
            vendor_name,
            is_non_po_item,
            part_skid_sequence,
            part_skid_total
        )
        SELECT
            COALESCE(rld.load_id, UUID())                              AS load_guid,
            rld.quantity                                               AS quantity,
            rld.part_id                                                AS part_id,
            rld.part_description                                       AS part_description,
            rld.po_number                                              AS po_number,
            rld.employee_number                                        AS employee_number,
            rld.heat                                                   AS heat,
            COALESCE(DATE(rld.received_date), rld.transaction_date)    AS transaction_date,
            rld.initial_location                                       AS initial_location,
            rld.coils_on_skid                                          AS coils_on_skid,
            COALESCE(rld.label_number, 1)                              AS label_number,
            COALESCE(rld.po_vendor, rld.vendor_name)                   AS vendor_name,
            COALESCE(rld.is_non_po_item, 0)                            AS is_non_po_item,
            rld.part_skid_sequence                                     AS part_skid_sequence,
            rld.part_skid_total                                        AS part_skid_total
        FROM receiving_label_data rld
        ON DUPLICATE KEY UPDATE
            quantity         = VALUES(quantity),
            part_id          = VALUES(part_id),
            part_description = VALUES(part_description),
            po_number        = VALUES(po_number),
            employee_number  = VALUES(employee_number),
            heat             = VALUES(heat),
            transaction_date = VALUES(transaction_date),
            initial_location = VALUES(initial_location),
            coils_on_skid    = VALUES(coils_on_skid),
            label_number     = VALUES(label_number),
            vendor_name      = VALUES(vendor_name),
            is_non_po_item   = VALUES(is_non_po_item),
            part_skid_sequence = VALUES(part_skid_sequence),
            part_skid_total  = VALUES(part_skid_total);

        DELETE FROM receiving_label_data;

        COMMIT;

        SET p_rows_moved = v_rows_to_move;
        SET p_status = 0;
        SET p_error_message = NULL;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_LabelData_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_LabelData_GetAll` ()   BEGIN
    SELECT
        load_id                       AS LoadID,
        part_id                       AS PartID,
        part_description              AS PartDescription,
        part_type                     AS PartType,
        po_number                     AS PONumber,
        po_line_number                AS POLineNumber,
        po_vendor                     AS POVendor,
        po_status                     AS POStatus,
        po_due_date                   AS PODueDate,
        qty_ordered                   AS QtyOrdered,
        unit_of_measure               AS UnitOfMeasure,
        remaining_quantity            AS RemainingQuantity,
        load_number                   AS LoadNumber,
        weight_quantity               AS WeightQuantity,
        heat                          AS HeatLotNumber,
        packages_per_load             AS PackagesPerLoad,
        package_type_name             AS PackageTypeName,
        weight_per_package            AS WeightPerPackage,
        is_non_po_item                AS IsNonPOItem,
        received_date                 AS ReceivedDate,
        user_id                       AS UserID,
        employee_number               AS EmployeeNumber,
        is_quality_hold_required      AS IsQualityHoldRequired,
        is_quality_hold_acknowledged  AS IsQualityHoldAcknowledged,
        quality_hold_restriction_type AS QualityHoldRestrictionType
    FROM receiving_label_data
    ORDER BY load_number ASC;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_LabelData_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_LabelData_Insert` (IN `p_load_id` CHAR(36), IN `p_load_number` INT, IN `p_quantity` INT, IN `p_weight_quantity` DECIMAL(18,2), IN `p_part_id` VARCHAR(50), IN `p_part_description` VARCHAR(500), IN `p_part_type` VARCHAR(50), IN `p_po_number` VARCHAR(20), IN `p_po_line_number` VARCHAR(10), IN `p_po_vendor` VARCHAR(255), IN `p_po_status` VARCHAR(100), IN `p_po_due_date` DATE, IN `p_qty_ordered` DECIMAL(18,2), IN `p_unit_of_measure` VARCHAR(20), IN `p_remaining_quantity` INT, IN `p_employee_number` INT, IN `p_user_id` VARCHAR(100), IN `p_heat` VARCHAR(100), IN `p_received_date` DATETIME, IN `p_transaction_date` DATE, IN `p_initial_location` VARCHAR(50), IN `p_packages_per_load` INT, IN `p_package_type_name` VARCHAR(50), IN `p_weight_per_package` DECIMAL(18,2), IN `p_coils_on_skid` INT, IN `p_label_number` INT, IN `p_vendor_name` VARCHAR(255), IN `p_is_non_po_item` TINYINT(1), IN `p_is_quality_hold_required` TINYINT(1), IN `p_is_quality_hold_acknowledged` TINYINT(1), IN `p_quality_hold_restriction_type` VARCHAR(255), IN `p_part_skid_sequence` INT, IN `p_part_skid_total` INT)   BEGIN
    INSERT IGNORE INTO receiving_label_data
    (
        load_id,
        load_number,
        quantity,
        weight_quantity,
        part_id,
        part_description,
        part_type,
        po_number,
        po_line_number,
        po_vendor,
        po_status,
        po_due_date,
        qty_ordered,
        unit_of_measure,
        remaining_quantity,
        employee_number,
        user_id,
        heat,
        received_date,
        transaction_date,
        initial_location,
        packages_per_load,
        package_type_name,
        weight_per_package,
        coils_on_skid,
        label_number,
        vendor_name,
        is_non_po_item,
        is_quality_hold_required,
        is_quality_hold_acknowledged,
        quality_hold_restriction_type,
        part_skid_sequence,
        part_skid_total
    )
    VALUES
    (
        p_load_id,
        p_load_number,
        p_quantity,
        p_weight_quantity,
        p_part_id,
        p_part_description,
        p_part_type,
        p_po_number,
        p_po_line_number,
        p_po_vendor,
        p_po_status,
        p_po_due_date,
        p_qty_ordered,
        p_unit_of_measure,
        p_remaining_quantity,
        p_employee_number,
        p_user_id,
        p_heat,
        p_received_date,
        p_transaction_date,
        p_initial_location,
        p_packages_per_load,
        p_package_type_name,
        p_weight_per_package,
        p_coils_on_skid,
        p_label_number,
        p_vendor_name,
        p_is_non_po_item,
        p_is_quality_hold_required,
        p_is_quality_hold_acknowledged,
        p_quality_hold_restriction_type,
        p_part_skid_sequence,
        p_part_skid_total
    );
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_LabelData_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_LabelData_Update` (IN `p_load_id` CHAR(36), IN `p_load_number` INT, IN `p_quantity` INT, IN `p_weight_quantity` DECIMAL(18,2), IN `p_part_id` VARCHAR(50), IN `p_part_description` VARCHAR(500), IN `p_part_type` VARCHAR(50), IN `p_po_number` VARCHAR(20), IN `p_po_line_number` VARCHAR(10), IN `p_po_vendor` VARCHAR(255), IN `p_po_status` VARCHAR(100), IN `p_po_due_date` DATE, IN `p_qty_ordered` DECIMAL(18,2), IN `p_unit_of_measure` VARCHAR(20), IN `p_remaining_quantity` INT, IN `p_employee_number` INT, IN `p_user_id` VARCHAR(100), IN `p_heat` VARCHAR(100), IN `p_received_date` DATETIME, IN `p_transaction_date` DATE, IN `p_initial_location` VARCHAR(50), IN `p_packages_per_load` INT, IN `p_package_type_name` VARCHAR(50), IN `p_weight_per_package` DECIMAL(18,2), IN `p_coils_on_skid` INT, IN `p_label_number` INT, IN `p_vendor_name` VARCHAR(255), IN `p_is_non_po_item` TINYINT(1), IN `p_is_quality_hold_required` TINYINT(1), IN `p_is_quality_hold_acknowledged` TINYINT(1), IN `p_quality_hold_restriction_type` VARCHAR(255))   BEGIN
    UPDATE receiving_label_data
    SET
        load_number = p_load_number,
        quantity = p_quantity,
        weight_quantity = p_weight_quantity,
        part_id = p_part_id,
        part_description = p_part_description,
        part_type = p_part_type,
        po_number = p_po_number,
        po_line_number = p_po_line_number,
        po_vendor = p_po_vendor,
        po_status = p_po_status,
        po_due_date = p_po_due_date,
        qty_ordered = p_qty_ordered,
        unit_of_measure = p_unit_of_measure,
        remaining_quantity = p_remaining_quantity,
        employee_number = p_employee_number,
        user_id = p_user_id,
        heat = p_heat,
        received_date = p_received_date,
        transaction_date = p_transaction_date,
        initial_location = p_initial_location,
        packages_per_load = p_packages_per_load,
        package_type_name = p_package_type_name,
        weight_per_package = p_weight_per_package,
        coils_on_skid = p_coils_on_skid,
        label_number = p_label_number,
        vendor_name = p_vendor_name,
        is_non_po_item = p_is_non_po_item,
        is_quality_hold_required = p_is_quality_hold_required,
        is_quality_hold_acknowledged = p_is_quality_hold_acknowledged,
        quality_hold_restriction_type = p_quality_hold_restriction_type
    WHERE load_id = p_load_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Line_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Line_Insert` (IN `p_Quantity` INT, IN `p_PartID` VARCHAR(50), IN `p_PONumber` INT, IN `p_EmployeeNumber` INT, IN `p_Heat` VARCHAR(100), IN `p_Date` DATE, IN `p_InitialLocation` VARCHAR(50), IN `p_CoilsOnSkid` INT, IN `p_VendorName` VARCHAR(255), IN `p_PartDescription` VARCHAR(500), OUT `p_Status` INT, OUT `p_ErrorMsg` VARCHAR(500))   BEGIN
    
    SET p_Status = 1;
    SET p_ErrorMsg = 'sp_Receiving_Line_Insert is deprecated - use receiving_history table';
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Load_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Load_Delete` (IN `p_LoadID` CHAR(36))   BEGIN
    DELETE FROM receiving_history
    WHERE load_guid = p_LoadID;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Load_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Load_GetAll` (IN `p_StartDate` DATE, IN `p_EndDate` DATE)   BEGIN
    SELECT
        load_guid           AS LoadID,
        part_id             AS PartID,
        part_description    AS PartDescription,
        NULL                AS PartType,
        po_number           AS PONumber,
        NULL                AS POLineNumber,
        vendor_name         AS POVendor,
        NULL                AS POStatus,
        NULL                AS PODueDate,
        NULL                AS QtyOrdered,
        NULL                AS UnitOfMeasure,
        NULL                AS RemainingQuantity,
        label_number        AS LoadNumber,
        quantity            AS WeightQuantity,
        heat                AS HeatLotNumber,
        NULL                AS PackagesPerLoad,
        NULL                AS PackageTypeName,
        NULL                AS WeightPerPackage,
        is_non_po_item      AS IsNonPOItem,
        transaction_date    AS ReceivedDate,
        NULL                AS UserID,
        employee_number     AS EmployeeNumber,
        0                   AS IsQualityHoldRequired,
        0                   AS IsQualityHoldAcknowledged,
        NULL                AS QualityHoldRestrictionType
    FROM receiving_history
    WHERE transaction_date >= p_StartDate
      AND transaction_date <= p_EndDate
      AND part_id IS NOT NULL
      AND part_id != ''
    ORDER BY transaction_date DESC, label_number ASC;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Load_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Load_Insert` (IN `p_LoadGuid` CHAR(36), IN `p_Quantity` INT, IN `p_PartID` VARCHAR(50), IN `p_PONumber` VARCHAR(20), IN `p_EmployeeNumber` INT, IN `p_Heat` VARCHAR(100), IN `p_TransactionDate` DATE, IN `p_InitialLocation` VARCHAR(50), IN `p_CoilsOnSkid` INT, IN `p_LabelNumber` INT, IN `p_VendorName` VARCHAR(255), IN `p_PartDescription` VARCHAR(500))   BEGIN
    INSERT INTO receiving_history
    (
        load_guid,
        quantity,
        part_id,
        po_number,
        employee_number,
        heat,
        transaction_date,
        initial_location,
        coils_on_skid,
        label_number,
        vendor_name,
        part_description
    )
    VALUES
    (
        p_LoadGuid,
        p_Quantity,
        p_PartID,
        p_PONumber,
        p_EmployeeNumber,
        p_Heat,
        p_TransactionDate,
        p_InitialLocation,
        p_CoilsOnSkid,
        IFNULL(p_LabelNumber, 1),
        p_VendorName,
        p_PartDescription
    );

    SELECT LAST_INSERT_ID() AS new_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Load_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Load_Update` (IN `p_LoadID` CHAR(36), IN `p_PartID` VARCHAR(50), IN `p_PartType` VARCHAR(100), IN `p_PONumber` VARCHAR(20), IN `p_POLineNumber` VARCHAR(50), IN `p_LoadNumber` INT, IN `p_WeightQuantity` DECIMAL(18,4), IN `p_HeatLotNumber` VARCHAR(100), IN `p_PackagesPerLoad` INT, IN `p_PackageTypeName` VARCHAR(100), IN `p_WeightPerPackage` DECIMAL(18,4), IN `p_IsNonPOItem` TINYINT(1), IN `p_ReceivedDate` DATETIME)   BEGIN
    UPDATE receiving_history
    SET
        part_id          = p_PartID,
        po_number        = p_PONumber,
        quantity         = ROUND(p_WeightQuantity, 0),
        heat             = p_HeatLotNumber,
        transaction_date = DATE(p_ReceivedDate),
        label_number     = IFNULL(p_LoadNumber, 1),
        is_non_po_item   = IFNULL(p_IsNonPOItem, 0)
    WHERE load_guid = p_LoadID;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypeMappings_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypeMappings_Delete` (IN `p_id` INT)   proc_body: BEGIN
    DECLARE v_exists INT DEFAULT 0;
    DECLARE v_is_default TINYINT DEFAULT 0;

    SELECT COUNT(*) INTO v_exists
    FROM receiving_package_type_mapping
    WHERE id = p_id;

    IF v_exists = 0 THEN
        SELECT 0 AS affected_rows, 'NotFound' AS status, 'Mapping not found' AS message;
        LEAVE proc_body;
    END IF;

    SELECT IF(is_default,1,0) INTO v_is_default
    FROM receiving_package_type_mapping
    WHERE id = p_id
    LIMIT 1;

    IF v_is_default = 1 THEN
        SELECT 0 AS affected_rows, 'Forbidden' AS status, 'Cannot delete default mapping' AS message;
        LEAVE proc_body;
    END IF;

    UPDATE receiving_package_type_mapping
    SET is_active = FALSE,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows, 'OK' AS status, NULL AS message;
END proc_body$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypeMappings_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypeMappings_GetAll` (IN `p_includeInactive` BOOLEAN)   BEGIN
    SELECT
        id,
        part_prefix,
        package_type,
        is_default,
        display_order,
        is_active,
        created_at,
        updated_at,
        created_by
    FROM mtm_receiving_application.receiving_package_type_mapping
    WHERE (is_active = TRUE) OR (p_includeInactive = TRUE)
    ORDER BY display_order, part_prefix;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypeMappings_GetByPrefix`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypeMappings_GetByPrefix` (IN `p_part_identifier` VARCHAR(100))   sp_end: BEGIN
    DECLARE v_package_type VARCHAR(50) DEFAULT NULL;
    DECLARE v_input VARCHAR(100) DEFAULT NULL;

    SET v_input = TRIM(IFNULL(p_part_identifier, ''));

    
    IF v_input = '' THEN
        SELECT package_type
        INTO v_package_type
        FROM receiving_package_type_mapping
        WHERE is_default = TRUE
          AND is_active = TRUE
        LIMIT 1;

        SELECT v_package_type AS package_type;
        LEAVE sp_end;
    END IF;

    
    
    
    SELECT package_type
    INTO v_package_type
    FROM receiving_package_type_mapping
    WHERE is_active = TRUE
      AND v_input LIKE CONCAT(part_prefix, '%')
    ORDER BY CHAR_LENGTH(part_prefix) DESC, display_order ASC
    LIMIT 1;

    
    IF v_package_type IS NULL THEN
        SELECT package_type
        INTO v_package_type
        FROM receiving_package_type_mapping
        WHERE is_default = TRUE
          AND is_active = TRUE
        LIMIT 1;
    END IF;

    SELECT v_package_type AS package_type;

END sp_END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypeMappings_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypeMappings_Insert` (IN `p_part_prefix` VARCHAR(10), IN `p_package_type` VARCHAR(50), IN `p_is_default` TINYINT(1), IN `p_display_order` INT, IN `p_created_by` INT)   BEGIN
    DECLARE v_prefix VARCHAR(10);

    SET v_prefix = UPPER(TRIM(p_part_prefix));

    
    IF p_is_default = 1 THEN
        UPDATE receiving_package_type_mapping
        SET is_default = FALSE
        WHERE is_default = TRUE;
    END IF;

    
    
    INSERT INTO receiving_package_type_mapping (
        part_prefix,
        package_type,
        is_default,
        display_order,
        is_active,
        created_by
    ) VALUES (
        v_prefix,
        p_package_type,
        p_is_default,
        p_display_order,
        TRUE,
        p_created_by
    )
    ON DUPLICATE KEY UPDATE
        package_type = VALUES(package_type),
        is_default = VALUES(is_default),
        display_order = VALUES(display_order),
        is_active = TRUE,
        created_by = IFNULL(VALUES(created_by), created_by),
        updated_at = CURRENT_TIMESTAMP,
        id = LAST_INSERT_ID(id);

    SELECT LAST_INSERT_ID() AS id;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypeMappings_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypeMappings_Update` (IN `p_id` INT, IN `p_part_prefix` VARCHAR(10), IN `p_package_type` VARCHAR(50), IN `p_is_default` BOOLEAN, IN `p_display_order` INT, IN `p_is_active` BOOLEAN)   BEGIN
    DECLARE v_conflict_id INT DEFAULT NULL;

    
    SELECT id INTO v_conflict_id
    FROM receiving_package_type_mapping
    WHERE part_prefix = p_part_prefix
      AND id <> p_id
    LIMIT 1;

    IF v_conflict_id IS NOT NULL THEN
        SELECT 0 AS affected_rows, CONCAT('Duplicate part_prefix found with id=', v_conflict_id) AS error_message;
    ELSE
        
        IF p_is_default THEN
            UPDATE receiving_package_type_mapping
            SET is_default = FALSE
            WHERE is_default = TRUE
              AND id <> p_id;
        END IF;

        
        UPDATE receiving_package_type_mapping
        SET part_prefix = p_part_prefix,
            package_type = p_package_type,
            is_default = p_is_default,
            display_order = p_display_order,
            is_active = p_is_active,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = p_id;

        SELECT ROW_COUNT() AS affected_rows, NULL AS error_message;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypePreference_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypePreference_Delete` (IN `p_PartID` VARCHAR(50))   BEGIN
    DELETE FROM receiving_package_types WHERE PartID = p_PartID;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypePreference_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypePreference_Get` (IN `p_PartID` VARCHAR(50))   BEGIN
    SELECT * FROM receiving_package_types WHERE PartID = p_PartID;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypePreference_Save`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypePreference_Save` (IN `p_PartID` VARCHAR(50), IN `p_PackageTypeName` VARCHAR(50), IN `p_CustomTypeName` VARCHAR(50), IN `p_LastModified` DATETIME)   BEGIN
    INSERT INTO receiving_package_types (PartID, PackageTypeName, CustomTypeName, LastModified)
    VALUES (p_PartID, p_PackageTypeName, p_CustomTypeName, p_LastModified)
    ON DUPLICATE KEY UPDATE
        PackageTypeName = p_PackageTypeName,
        CustomTypeName = p_CustomTypeName,
        LastModified = p_LastModified;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_PackageTypes_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_PackageTypes_Delete` (IN `p_id` INT)   BEGIN
    
    
    
    DELETE FROM receiving_package_types
    WHERE PreferenceID = p_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_QualityHolds_GetByLoadID`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_QualityHolds_GetByLoadID` (IN `p_LoadID` INT, OUT `p_Status` INT, OUT `p_ErrorMsg` VARCHAR(500))   BEGIN
    SET p_Status = 1;
    SET p_ErrorMsg = '';

    
    IF p_LoadID IS NULL OR p_LoadID <= 0 THEN
        SET p_Status = 0;
        SET p_ErrorMsg = 'LoadID is required and must be positive';
        SELECT NULL AS quality_hold_id LIMIT 0;
    ELSE
        
        SELECT
            quality_hold_id,
            load_id,
            part_id,
            restriction_type,
            quality_acknowledged_by,
            quality_acknowledged_at,
            created_at
        FROM receiving_quality_holds
        WHERE load_id = p_LoadID
        ORDER BY created_at DESC;
    END IF;

END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_QualityHolds_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_QualityHolds_Insert` (IN `p_LoadID` INT, IN `p_PartID` VARCHAR(50), IN `p_RestrictionType` VARCHAR(50), IN `p_QualityAcknowledgedBy` VARCHAR(255), IN `p_QualityAcknowledgedAt` DATETIME, OUT `p_QualityHoldID` INT, OUT `p_Status` INT, OUT `p_ErrorMsg` VARCHAR(500))   BEGIN
    DECLARE v_RowCount INT;

    SET p_Status = 1;
    SET p_ErrorMsg = '';
    SET p_QualityHoldID = -1;

    
    IF p_LoadID IS NULL OR p_LoadID <= 0 THEN
        SET p_Status = 0;
        SET p_ErrorMsg = 'LoadID is required and must be positive';
    ELSEIF p_PartID IS NULL OR p_PartID = '' THEN
        SET p_Status = 0;
        SET p_ErrorMsg = 'PartID is required';
    ELSEIF p_RestrictionType IS NULL OR p_RestrictionType = '' THEN
        SET p_Status = 0;
        SET p_ErrorMsg = 'RestrictionType is required';
    ELSE
        
        INSERT INTO receiving_quality_holds (
            load_id,
            part_id,
            restriction_type,
            quality_acknowledged_by,
            quality_acknowledged_at,
            created_at
        ) VALUES (
            p_LoadID,
            p_PartID,
            p_RestrictionType,
            p_QualityAcknowledgedBy,
            p_QualityAcknowledgedAt,
            NOW()
        );

        SET v_RowCount = ROW_COUNT();

        IF v_RowCount > 0 THEN
            SET p_QualityHoldID = LAST_INSERT_ID();
            SET p_Status = 1;
        ELSE
            SET p_Status = 0;
            SET p_ErrorMsg = 'Failed to insert quality hold record';
        END IF;
    END IF;

END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_QualityHolds_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_QualityHolds_Update` (IN `p_QualityHoldID` INT, IN `p_QualityAcknowledgedBy` VARCHAR(255), IN `p_QualityAcknowledgedAt` DATETIME, OUT `p_Status` INT, OUT `p_ErrorMsg` VARCHAR(500))   BEGIN
    DECLARE v_RowCount INT;

    SET p_Status = 1;
    SET p_ErrorMsg = '';

    
    IF p_QualityHoldID IS NULL OR p_QualityHoldID <= 0 THEN
        SET p_Status = 0;
        SET p_ErrorMsg = 'QualityHoldID is required and must be positive';
    ELSE
        
        UPDATE receiving_quality_holds
        SET
            quality_acknowledged_by = p_QualityAcknowledgedBy,
            quality_acknowledged_at = p_QualityAcknowledgedAt,
            updated_at = NOW()
        WHERE quality_hold_id = p_QualityHoldID;

        SET v_RowCount = ROW_COUNT();

        IF v_RowCount > 0 THEN
            SET p_Status = 1;
        ELSE
            SET p_Status = 0;
            SET p_ErrorMsg = 'Quality hold record not found';
        END IF;
    END IF;

END$$

DROP PROCEDURE IF EXISTS `sp_Reporting_Availability_GetByDateRange`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Reporting_Availability_GetByDateRange` (IN `p_start_date` DATE, IN `p_end_date` DATE)   BEGIN
    SELECT
        (SELECT COUNT(*) FROM view_receiving_history WHERE created_date BETWEEN p_start_date AND p_end_date) AS receiving_count,
        (SELECT COUNT(*) FROM view_dunnage_history WHERE created_date BETWEEN p_start_date AND p_end_date) AS dunnage_count,
    (SELECT COUNT(*) FROM view_volvo_history WHERE DATE(created_date) BETWEEN p_start_date AND p_end_date) AS volvo_count;
END$$

DROP PROCEDURE IF EXISTS `sp_Reporting_DunnageHistory_GetByDateRange`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Reporting_DunnageHistory_GetByDateRange` (IN `p_start_date` DATE, IN `p_end_date` DATE)   BEGIN
    SELECT
        id,
        po_number,
        dunnage_type,
        part_number,
        quantity,
        created_date,
        employee_number,
        created_by_username,
        source_module,
        location,
        notes,
        NULL AS load_number,
        NULL AS label_number,
        NULL AS packages_per_load,
        NULL AS package_type_name,
        NULL AS coils_on_skid,
        NULL AS quantity_per_skid,
        NULL AS received_skid_count
    FROM view_dunnage_history
    WHERE created_date BETWEEN p_start_date AND p_end_date
    ORDER BY created_date DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Reporting_ReceivingHistory_GetByDateRange`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Reporting_ReceivingHistory_GetByDateRange` (IN `p_start_date` DATE, IN `p_end_date` DATE)   BEGIN
    SELECT
        id,
        po_number,
        po_line_number,
        part_number,
        quantity,
        created_date,
        employee_number,
        NULL AS created_by_username,
        source_module,
        initial_location AS location,
        notes,
        load_number,
        label_number,
        packages_per_load,
        package_type_name,
        coils_on_skid,
        is_non_po_item,
        NULL AS quantity_per_skid,
        NULL AS received_skid_count
    FROM view_receiving_history
    WHERE created_date BETWEEN p_start_date AND p_end_date
    ORDER BY created_date DESC, id DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Reporting_VolvoHistory_GetByDateRange`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Reporting_VolvoHistory_GetByDateRange` (IN `p_start_date` DATE, IN `p_end_date` DATE)   BEGIN
    SELECT
        id,
        po_number,
        part_number,
        quantity,
        employee_number,
        notes,
        created_date,
        NULL AS created_by_username,
        source_module,
        location,
        NULL AS load_number,
        NULL AS label_number,
        NULL AS packages_per_load,
        NULL AS package_type_name,
        NULL AS coils_on_skid,
        quantity_per_skid,
        received_skid_count,
        shipment_number,
        receiver_number,
        status,
        part_count
    FROM view_volvo_history
    WHERE DATE(created_date) BETWEEN p_start_date AND p_end_date
    ORDER BY created_date DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Audit_GetBySetting`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Audit_GetBySetting` (IN `p_category` VARCHAR(100), IN `p_setting_key` VARCHAR(150))   BEGIN
    SELECT *
    FROM settings_activity
    WHERE category = p_category AND setting_key = p_setting_key
    ORDER BY changed_at DESC
    LIMIT 100;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Audit_GetByUser`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Audit_GetByUser` (IN `p_user_id` INT)   BEGIN
    SELECT *
    FROM settings_activity
    WHERE user_id = p_user_id
    ORDER BY changed_at DESC
    LIMIT 100;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Audit_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Audit_Insert` (IN `p_scope` VARCHAR(20), IN `p_category` VARCHAR(100), IN `p_setting_key` VARCHAR(150), IN `p_old_value` TEXT, IN `p_new_value` TEXT, IN `p_change_type` VARCHAR(50), IN `p_user_id` INT, IN `p_changed_by` VARCHAR(100), IN `p_ip_address` VARCHAR(50), IN `p_workstation` VARCHAR(100))   BEGIN
    INSERT INTO settings_activity (
        scope, category, setting_key, old_value, new_value, change_type, user_id, changed_by, changed_at, ip_address, workstation
    ) VALUES (
        p_scope, p_category, p_setting_key, p_old_value, p_new_value, p_change_type, p_user_id, p_changed_by, NOW(), p_ip_address, p_workstation
    );
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Meta_GetStoredProcedures`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Meta_GetStoredProcedures` ()   BEGIN
    SELECT routine_name AS procedure_name
    FROM information_schema.routines
    WHERE routine_schema = DATABASE()
      AND routine_type = 'PROCEDURE'
      AND routine_name LIKE 'sp_SettingsCore_%';
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Meta_GetTables`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Meta_GetTables` ()   BEGIN
    SELECT table_name
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name IN (
        'settings_universal',
        'settings_personal',
        'settings_activity',
        'settings_roles',
        'settings_user_roles'
      );
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Roles_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Roles_GetAll` ()   BEGIN
    SELECT * FROM settings_roles ORDER BY role_name;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_Roles_GetByName`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_Roles_GetByName` (IN `p_role_name` VARCHAR(100))   BEGIN
    SELECT 
        id,
        role_name,
        description,
        created_at
    FROM settings_roles
    WHERE role_name = p_role_name
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_System_GetByCategory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_System_GetByCategory` (IN `p_category` VARCHAR(100))   BEGIN
    SELECT *
    FROM settings_universal
    WHERE category = p_category;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_System_GetByKey`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_System_GetByKey` (IN `p_category` VARCHAR(100), IN `p_key` VARCHAR(150))   BEGIN
    SELECT *
    FROM settings_universal
    WHERE category = p_category AND setting_key = p_key
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_System_Reset`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_System_Reset` (IN `p_category` VARCHAR(100), IN `p_key` VARCHAR(150), IN `p_updated_by` VARCHAR(100))   BEGIN
    UPDATE settings_universal
    SET updated_by = p_updated_by,
        updated_at = NOW()
    WHERE category = p_category AND setting_key = p_key;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_System_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_System_Upsert` (IN `p_category` VARCHAR(100), IN `p_key` VARCHAR(150), IN `p_value` TEXT, IN `p_data_type` VARCHAR(50), IN `p_is_sensitive` TINYINT(1), IN `p_updated_by` VARCHAR(100))   BEGIN
    INSERT INTO settings_universal (
        category, setting_key, setting_value, data_type, is_sensitive, is_locked, updated_by, updated_at
    ) VALUES (
        p_category, p_key, p_value, p_data_type, p_is_sensitive, 0, p_updated_by, NOW()
    )
    ON DUPLICATE KEY UPDATE
        setting_value = VALUES(setting_value),
        data_type = VALUES(data_type),
        is_sensitive = VALUES(is_sensitive),
        updated_by = VALUES(updated_by),
        updated_at = NOW();
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_UserRoles_Assign`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_UserRoles_Assign` (IN `p_user_id` INT, IN `p_role_id` INT)   BEGIN
    
    IF NOT EXISTS (SELECT 1 FROM settings_user_roles 
                   WHERE user_id = p_user_id AND role_id = p_role_id)
    THEN
        
        BEGIN
            DECLARE EXIT HANDLER FOR SQLEXCEPTION
            BEGIN
                
                
            END;
            
            INSERT INTO settings_user_roles (user_id, role_id, assigned_at)
            VALUES (p_user_id, p_role_id, NOW());
        END;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_UserRoles_GetByUser`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_UserRoles_GetByUser` (IN `p_user_id` INT)   BEGIN
    SELECT * FROM settings_user_roles WHERE user_id = p_user_id;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_User_GetByCategory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_User_GetByCategory` (IN `p_user_id` INT, IN `p_category` VARCHAR(100))   BEGIN
    SELECT *
    FROM settings_personal
    WHERE user_id = p_user_id AND category = p_category;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_User_GetByKey`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_User_GetByKey` (IN `p_user_id` INT, IN `p_category` VARCHAR(100), IN `p_key` VARCHAR(150))   BEGIN
    SELECT *
    FROM settings_personal
    WHERE user_id = p_user_id AND category = p_category AND setting_key = p_key
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_User_Reset`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_User_Reset` (IN `p_user_id` INT, IN `p_category` VARCHAR(100), IN `p_key` VARCHAR(150), IN `p_updated_by` VARCHAR(100))   BEGIN
    UPDATE settings_personal
    SET updated_by = p_updated_by,
        updated_at = NOW()
    WHERE user_id = p_user_id AND category = p_category AND setting_key = p_key;
END$$

DROP PROCEDURE IF EXISTS `sp_SettingsCore_User_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_SettingsCore_User_Upsert` (IN `p_user_id` INT, IN `p_category` VARCHAR(100), IN `p_key` VARCHAR(150), IN `p_value` TEXT, IN `p_data_type` VARCHAR(50), IN `p_updated_by` VARCHAR(100))   BEGIN
    INSERT INTO settings_personal (
        user_id, category, setting_key, setting_value, data_type, updated_by, updated_at
    ) VALUES (
        p_user_id, p_category, p_key, p_value, p_data_type, p_updated_by, NOW()
    )
    ON DUPLICATE KEY UPDATE
        setting_value = VALUES(setting_value),
        data_type = VALUES(data_type),
        updated_by = VALUES(updated_by),
        updated_at = NOW();
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_AuditLog_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_AuditLog_Get` (IN `p_setting_id` INT, IN `p_limit` INT)   BEGIN
    
    
    IF p_limit IS NULL OR p_limit <= 0 THEN
        SELECT
            sal.id,
            sal.setting_id,
            ss.category,
            ss.setting_key,
            ss.setting_name,
            sal.user_setting_id,
            sal.old_value,
            sal.new_value,
            sal.change_type,
            sal.changed_by,
            sal.changed_at,
            sal.ip_address,
            sal.workstation_name
        FROM settings_activity sal
        INNER JOIN settings_universal ss ON sal.setting_id = ss.id
        WHERE (p_setting_id IS NULL OR sal.setting_id = p_setting_id)
        ORDER BY sal.changed_at DESC;
    ELSE
        SELECT
            sal.id,
            sal.setting_id,
            ss.category,
            ss.setting_key,
            ss.setting_name,
            sal.user_setting_id,
            sal.old_value,
            sal.new_value,
            sal.change_type,
            sal.changed_by,
            sal.changed_at,
            sal.ip_address,
            sal.workstation_name
        FROM settings_activity sal
        INNER JOIN settings_universal ss ON sal.setting_id = ss.id
        WHERE (p_setting_id IS NULL OR sal.setting_id = p_setting_id)
        ORDER BY sal.changed_at DESC
        LIMIT p_limit;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_AuditLog_GetBySetting`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_AuditLog_GetBySetting` (IN `p_setting_id` INT, IN `p_limit` INT)   BEGIN
    
    DECLARE l_limit INT DEFAULT 100;
    IF p_limit IS NOT NULL AND p_limit > 0 THEN
        SET l_limit = p_limit;
    END IF;

    SELECT
        sal.id,
        sal.setting_id,
        ss.category,
        ss.setting_key,
        ss.setting_name,
        sal.user_setting_id,
        sal.old_value,
        sal.new_value,
        sal.change_type,
        sal.changed_by,
        sal.changed_at,
        sal.ip_address,
        sal.workstation_name
    FROM settings_activity sal
    INNER JOIN settings_universal ss ON sal.setting_id = ss.id
    WHERE sal.setting_id = p_setting_id
    ORDER BY sal.changed_at DESC
    LIMIT l_limit;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_AuditLog_GetByUser`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_AuditLog_GetByUser` (IN `p_changed_by` INT, IN `p_limit` INT)   BEGIN
    
    
    IF p_limit IS NULL OR p_limit <= 0 THEN
        SELECT
            sal.id,
            sal.setting_id,
            ss.category,
            ss.setting_key,
            ss.setting_name,
            sal.user_setting_id,
            sal.old_value,
            sal.new_value,
            sal.change_type,
            sal.changed_by,
            sal.changed_at,
            sal.ip_address,
            sal.workstation_name
        FROM settings_activity sal
        INNER JOIN settings_universal ss ON sal.setting_id = ss.id
        WHERE sal.changed_by = p_changed_by
        ORDER BY sal.changed_at DESC;
    ELSE
        SET @sql = CONCAT(
            'SELECT
                sal.id,
                sal.setting_id,
                ss.category,
                ss.setting_key,
                ss.setting_name,
                sal.user_setting_id,
                sal.old_value,
                sal.new_value,
                sal.change_type,
                sal.changed_by,
                sal.changed_at,
                sal.ip_address,
                sal.workstation_name
            FROM settings_activity sal
            INNER JOIN settings_universal ss ON sal.setting_id = ss.id
            WHERE sal.changed_by = ? 
            ORDER BY sal.changed_at DESC
            LIMIT ',
            p_limit
        );
        PREPARE stmt FROM @sql;
        SET @p_changed_by = p_changed_by;
        EXECUTE stmt USING @p_changed_by;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_Delete` (IN `p_id` INT)   BEGIN
    
    UPDATE reporting_scheduled_reports
    SET is_active = 0,
        updated_at = NOW()
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_GetActive`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_GetActive` ()   BEGIN
    SELECT
        id,
        report_type,
        schedule,
        email_recipients,
        next_run_date,
        last_run_date
    FROM reporting_scheduled_reports
    WHERE is_active = 1
      AND next_run_date IS NOT NULL
    ORDER BY next_run_date ASC;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_GetAll` ()   BEGIN
    SELECT
        `id`,
        `report_type`,
        `schedule`,
        `email_recipients`,
        `is_active`,
        `next_run_date`,
        `last_run_date`,
        `created_at`,
        `updated_at`,
        `created_by`
    FROM `reporting_scheduled_reports`
    WHERE `is_active` = 1
    ORDER BY `next_run_date` ASC, `report_type`;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_GetById` (IN `p_id` INT)   BEGIN
    SELECT
        id,
        report_type,
        schedule,
        email_recipients,
        is_active,
        next_run_date,
        last_run_date,
        created_at,
        updated_at,
        created_by
    FROM reporting_scheduled_reports
    WHERE id = p_id
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_GetDue`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_GetDue` ()   BEGIN
    SELECT
        id,
        report_type,
        schedule,
        email_recipients,
        next_run_date,
        last_run_date
    FROM reporting_scheduled_reports
    WHERE is_active = 1
      AND next_run_date IS NOT NULL
      AND next_run_date <= NOW()
    ORDER BY next_run_date ASC;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_Insert` (IN `p_report_type` VARCHAR(50), IN `p_schedule` VARCHAR(100), IN `p_email_recipients` VARCHAR(1000), IN `p_next_run_date` DATETIME, IN `p_created_by` INT)   BEGIN
    INSERT INTO reporting_scheduled_reports (
        report_type,
        schedule,
        email_recipients,
        is_active,
        next_run_date,
        created_by
    ) VALUES (
        p_report_type,
        p_schedule,
        p_email_recipients,
        1,
        p_next_run_date,
        p_created_by
    );

    SELECT LAST_INSERT_ID() AS id;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_ToggleActive`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_ToggleActive` (IN `p_id` INT, IN `p_is_active` TINYINT(1))   BEGIN
    UPDATE reporting_scheduled_reports
    SET is_active = p_is_active,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_Update` (IN `p_id` INT, IN `p_report_type` VARCHAR(50), IN `p_schedule` VARCHAR(100), IN `p_email_recipients` VARCHAR(1000), IN `p_next_run_date` DATETIME)   BEGIN
    UPDATE reporting_scheduled_reports
    SET report_type = p_report_type,
        schedule = p_schedule,
        email_recipients = p_email_recipients,
        next_run_date = p_next_run_date,
        updated_at = NOW()
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_ScheduledReport_UpdateLastRun`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_ScheduledReport_UpdateLastRun` (IN `p_id` INT, IN `p_last_run_date` DATETIME, IN `p_next_run_date` DATETIME, OUT `p_affected_rows` INT)   BEGIN
    UPDATE reporting_scheduled_reports
    SET last_run_date = p_last_run_date,
        next_run_date = p_next_run_date,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_id;

    
    SET p_affected_rows = ROW_COUNT();
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_System_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_System_GetAll` ()   BEGIN
    SELECT
        id,
        category,
        sub_category,
        setting_key,
        setting_name,
        description,
        setting_value,
        default_value,
        data_type,
        scope,
        permission_level,
        is_locked,
        is_sensitive,
        CAST(validation_rules AS CHAR) AS validation_rules,
        ui_control_type,
        ui_order,
        created_at,
        updated_at,
        updated_by
    FROM settings_universal
    ORDER BY category, ui_order, setting_name;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_System_GetByCategory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_System_GetByCategory` (IN `p_category` VARCHAR(50))   BEGIN
    SELECT
        `id`,
        `category`,
        `sub_category`,
        `setting_key`,
        `setting_name`,
        `description`,
        `setting_value`,
        `default_value`,
        `data_type`,
        `scope`,
        `permission_level`,
        `is_locked`,
        `is_sensitive`,
        `validation_rules`,
        `ui_control_type`,
        `ui_order`,
        `created_at`,
        `updated_at`,
        `updated_by`
    FROM `settings_universal`
    WHERE `category` = p_category
    ORDER BY `ui_order`, `setting_name`;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_System_GetByKey`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_System_GetByKey` (IN `p_category` VARCHAR(50), IN `p_setting_key` VARCHAR(100))   BEGIN
    SELECT
        id,
        category,
        sub_category,
        setting_key,
        setting_name,
        description,
        setting_value,
        default_value,
        data_type,
        scope,
        permission_level,
        is_locked,
        is_sensitive,
        CAST(validation_rules AS CHAR) AS validation_rules,
        ui_control_type,
        ui_order,
        created_at,
        updated_at,
        updated_by
    FROM settings_universal
    WHERE category = p_category
      AND setting_key = p_setting_key
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_System_ResetToDefault`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_System_ResetToDefault` (IN `p_setting_id` INT, IN `p_changed_by` INT, IN `p_ip_address` VARCHAR(45), IN `p_workstation_name` VARCHAR(100))   BEGIN
    
    DECLARE v_old_value VARCHAR(4000);
    DECLARE v_default_value VARCHAR(4000);
    DECLARE v_is_locked TINYINT(1);
    DECLARE v_exists INT DEFAULT 0;

    
    SELECT COUNT(*) INTO v_exists FROM settings_universal WHERE id = p_setting_id;
    IF v_exists = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Setting not found';
    END IF;

    
    SELECT setting_value, default_value, is_locked
    INTO v_old_value, v_default_value, v_is_locked
    FROM settings_universal
    WHERE id = p_setting_id
    LIMIT 1;

    
    IF COALESCE(v_is_locked, 0) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Setting is locked and cannot be reset';
    END IF;

    
    UPDATE settings_universal
    SET setting_value = v_default_value,
        updated_at = CURRENT_TIMESTAMP,
        updated_by = p_changed_by
    WHERE id = p_setting_id;

    
    INSERT INTO settings_activity (
        setting_id,
        old_value,
        new_value,
        change_type,
        changed_by,
        ip_address,
        workstation_name
    ) VALUES (
        p_setting_id,
        v_old_value,
        v_default_value,
        'reset',
        p_changed_by,
        p_ip_address,
        p_workstation_name
    );

    SELECT 1 AS success;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_System_SetLocked`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_System_SetLocked` (IN `p_setting_id` INT, IN `p_is_locked` TINYINT(1), IN `p_changed_by` INT, IN `p_ip_address` VARCHAR(45), IN `p_workstation_name` VARCHAR(100))   proc_main: BEGIN
    DECLARE v_old_value VARCHAR(10) DEFAULT NULL;

    START TRANSACTION;

    
    SELECT CASE WHEN is_locked THEN 'locked' ELSE 'unlocked' END
      INTO v_old_value
      FROM settings_universal
     WHERE id = p_setting_id
     FOR UPDATE;

    IF v_old_value IS NULL THEN
        ROLLBACK;
        SELECT 0 AS success, 'Setting not found' AS message;
        LEAVE proc_main;
    END IF;

    UPDATE settings_universal
    SET is_locked = p_is_locked,
        updated_at = CURRENT_TIMESTAMP,
        updated_by = p_changed_by
    WHERE id = p_setting_id;

    
    INSERT INTO settings_activity (
        setting_id,
        old_value,
        new_value,
        change_type,
        changed_by,
        ip_address,
        workstation_name
    ) VALUES (
        p_setting_id,
        v_old_value,
        CASE WHEN p_is_locked THEN 'locked' ELSE 'unlocked' END,
        CASE WHEN p_is_locked THEN 'lock' ELSE 'unlock' END,
        p_changed_by,
        p_ip_address,
        p_workstation_name
    );

    COMMIT;

    SELECT 1 AS success, 'OK' AS message;
END proc_main$$

DROP PROCEDURE IF EXISTS `sp_Settings_System_UpdateValue`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_System_UpdateValue` (IN `p_setting_id` INT, IN `p_new_value` TEXT, IN `p_changed_by` INT, IN `p_ip_address` VARCHAR(45), IN `p_workstation_name` VARCHAR(100))   BEGIN
    DECLARE v_old_value TEXT;
    DECLARE v_is_locked TINYINT(1) DEFAULT 0;

    
    SELECT setting_value, is_locked
    INTO v_old_value, v_is_locked
    FROM settings_universal
    WHERE id = p_setting_id;

    IF ROW_COUNT() = 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Setting not found';
    END IF;

    
    IF COALESCE(v_is_locked, 0) = 1 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Setting is locked and cannot be modified';
    END IF;

    
    UPDATE settings_universal
    SET setting_value = p_new_value,
        updated_at = CURRENT_TIMESTAMP,
        updated_by = p_changed_by
    WHERE id = p_setting_id;

    
    INSERT INTO settings_activity (
        setting_id,
        old_value,
        new_value,
        change_type,
        changed_by,
        ip_address,
        workstation_name
    ) VALUES (
        p_setting_id,
        v_old_value,
        p_new_value,
        'update',
        p_changed_by,
        p_ip_address,
        p_workstation_name
    );

    SELECT 1 AS success;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_User_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_User_Get` (IN `p_user_id` INT, IN `p_category` VARCHAR(50), IN `p_setting_key` VARCHAR(100))   BEGIN
    SELECT
        ss.id AS setting_id,
        ss.category,
        ss.setting_key,
        ss.setting_name,
        ss.description,
        COALESCE(us.setting_value, ss.setting_value, ss.default_value) AS effective_value,
        ss.default_value AS system_default,
        us.setting_value AS user_override,
        ss.data_type,
        ss.validation_rules,
        ss.ui_control_type,
        (us.id IS NOT NULL) AS has_override
    FROM settings_universal ss
    LEFT JOIN settings_personal us ON ss.id = us.setting_id AND us.user_id = p_user_id
    WHERE ss.category = p_category
      AND ss.setting_key = p_setting_key
      AND ss.scope = 'user';
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_User_GetAllForUser`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_User_GetAllForUser` (IN `p_user_id` INT)   BEGIN
    SELECT
        ss.id AS setting_id,
        ss.category,
        ss.setting_key,
        ss.setting_name,
        ss.description,
        IFNULL(us.setting_value, ss.setting_value) AS effective_value,
        ss.setting_value AS system_default,
        us.setting_value AS user_override,
        ss.data_type,
        CAST(ss.validation_rules AS CHAR) AS validation_rules,
        ss.ui_control_type,
        ss.ui_order,
        IF(us.id IS NOT NULL, 1, 0) AS has_override
    FROM settings_universal ss
    LEFT JOIN settings_personal us ON ss.id = us.setting_id AND us.user_id = p_user_id
    WHERE ss.scope = 'user'
    ORDER BY ss.category, ss.ui_order, ss.setting_name;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_User_Reset`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_User_Reset` (IN `p_user_id` INT, IN `p_setting_id` INT)   BEGIN
    DECLARE v_user_setting_id INT DEFAULT NULL;
    DECLARE v_old_value TEXT DEFAULT NULL;

    
    SELECT id, setting_value
    INTO v_user_setting_id, v_old_value
    FROM settings_personal
    WHERE user_id = p_user_id
      AND setting_id = p_setting_id
    LIMIT 1;

    IF v_user_setting_id IS NOT NULL THEN
        
        DELETE FROM settings_personal
        WHERE id = v_user_setting_id;

        
        INSERT INTO settings_activity (
            setting_id,
            user_setting_id,
            old_value,
            new_value,
            change_type,
            changed_by,
            changed_at
        ) VALUES (
            p_setting_id,
            v_user_setting_id,
            v_old_value,
            NULL,
            'reset',
            p_user_id,
            CURRENT_TIMESTAMP
        );
    END IF;

    SELECT 1 AS success;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_User_ResetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_User_ResetAll` (IN `p_user_id` INT, IN `p_changed_by` INT)   BEGIN
    
    INSERT INTO settings_activity (
        setting_id,
        user_setting_id,
        old_value,
        new_value,
        change_type,
        changed_by,
        changed_at
    )
    SELECT
        us.setting_id,
        us.id,
        us.setting_value,
        NULL,
        'reset',
        p_changed_by,
        CURRENT_TIMESTAMP
    FROM settings_personal us
    WHERE us.user_id = p_user_id;

    
    DELETE FROM settings_personal
    WHERE user_id = p_user_id;

    SELECT ROW_COUNT() AS reset_count;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_User_Set`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_User_Set` (IN `p_user_id` INT, IN `p_setting_id` INT, IN `p_setting_value` TEXT)   BEGIN
    DECLARE v_existing_id INT DEFAULT NULL;
    DECLARE v_old_value TEXT DEFAULT NULL;

    
    DECLARE CONTINUE HANDLER FOR NOT FOUND
    BEGIN
        SET v_existing_id = NULL;
        SET v_old_value = NULL;
    END;

    
    SELECT id, setting_value
    INTO v_existing_id, v_old_value
    FROM settings_personal
    WHERE user_id = p_user_id
      AND setting_id = p_setting_id
    LIMIT 1;

    IF v_existing_id IS NULL THEN
        
        INSERT INTO settings_personal (user_id, setting_id, setting_value)
        VALUES (p_user_id, p_setting_id, p_setting_value);

        SET v_existing_id = LAST_INSERT_ID();
    ELSE
        
        UPDATE settings_personal
        SET setting_value = p_setting_value,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = v_existing_id;
    END IF;

    
    INSERT INTO settings_activity (
        setting_id,
        user_setting_id,
        old_value,
        new_value,
        change_type,
        changed_by,
        changed_at
    ) VALUES (
        p_setting_id,
        v_existing_id,
        v_old_value,
        p_setting_value,
        IF(v_old_value IS NULL, 'create', 'update'),
        p_user_id,
        CURRENT_TIMESTAMP
    );

    SELECT 1 AS success, v_existing_id AS user_setting_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_LabelData_ClearToHistory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_LabelData_ClearToHistory` (IN `p_archived_by` VARCHAR(100), OUT `p_headers_moved` INT, OUT `p_lines_moved` INT, OUT `p_archive_batch_id` CHAR(36), OUT `p_status` INT, OUT `p_error_message` VARCHAR(1000))   BEGIN
    DECLARE v_completed_count INT DEFAULT 0;
    DECLARE v_lines_count     INT DEFAULT 0;

    
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        SET p_headers_moved    = 0;
        SET p_lines_moved      = 0;
        SET p_status           = 1;
        SET p_error_message    = 'Clear Label Data failed. Transaction rolled back.';
    END;

    SET p_headers_moved    = 0;
    SET p_lines_moved      = 0;
    SET p_status           = 0;
    SET p_error_message    = NULL;
    SET p_archive_batch_id = UUID();

    START TRANSACTION;

    
    SELECT COUNT(*) INTO v_completed_count
    FROM volvo_label_data
    WHERE status = 'completed';

    IF v_completed_count = 0 THEN
        
        COMMIT;
    ELSE
        
        SELECT COUNT(*) INTO v_lines_count
        FROM volvo_line_data vld
        WHERE vld.shipment_id IN (
            SELECT id FROM volvo_label_data WHERE status = 'completed'
        );

        
        
        
        
        INSERT INTO volvo_line_history (
            original_id,
            shipment_history_id,
            original_shipment_id,
            part_number,
            location,
            quantity_per_skid,
            received_skid_count,
            calculated_piece_count,
            has_discrepancy,
            expected_skid_count,
            discrepancy_note,
            archived_at,
            archived_by,
            archive_batch_id
        )
        SELECT
            vld.id                          AS original_id,
            
            
            
            
            0,                              
            vld.shipment_id                 AS original_shipment_id,
            vld.part_number,
            vld.location,
            vld.quantity_per_skid,
            vld.received_skid_count,
            vld.calculated_piece_count,
            vld.has_discrepancy,
            vld.expected_skid_count,
            vld.discrepancy_note,
            NOW()                           AS archived_at,
            p_archived_by                   AS archived_by,
            p_archive_batch_id              AS archive_batch_id
        FROM volvo_line_data vld
        WHERE vld.shipment_id IN (
            SELECT id FROM volvo_label_data WHERE status = 'completed'
        );
        

        
        
        
        INSERT INTO volvo_label_history (
            original_id,
            shipment_date,
            shipment_number,
            po_number,
            receiver_number,
            employee_number,
            notes,
            status,
            created_date,
            modified_date,
            archived_at,
            archived_by,
            archive_batch_id
        )
        SELECT
            vl.id               AS original_id,
            vl.shipment_date,
            vl.shipment_number,
            vl.po_number,
            vl.receiver_number,
            vl.employee_number,
            vl.notes,
            vl.status,
            vl.created_date,
            vl.modified_date,
            NOW()               AS archived_at,
            p_archived_by       AS archived_by,
            p_archive_batch_id  AS archive_batch_id
        FROM volvo_label_data vl
        WHERE vl.status = 'completed';

        
        
        
        
        UPDATE volvo_line_history vlh
        INNER JOIN volvo_label_history vlhdr
            ON  vlhdr.original_id      = vlh.original_shipment_id
            AND vlhdr.archive_batch_id = p_archive_batch_id
        SET vlh.shipment_history_id = vlhdr.id
        WHERE vlh.archive_batch_id = p_archive_batch_id
          AND vlh.shipment_history_id = 0;

        
        
        
        DELETE FROM volvo_line_data
        WHERE shipment_id IN (
            SELECT id FROM volvo_label_data WHERE status = 'completed'
        );

        
        
        
        DELETE FROM volvo_label_data
        WHERE status = 'completed';

        SET p_headers_moved = v_completed_count;
        SET p_lines_moved   = v_lines_count;

        COMMIT;
    END IF;

END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartComponent_DeleteByParent`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartComponent_DeleteByParent` (IN `p_parent_part_number` VARCHAR(20))   BEGIN
  DELETE FROM volvo_part_components
  WHERE parent_part_number = p_parent_part_number;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartComponent_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartComponent_Get` (IN `p_parent_part_number` VARCHAR(20))   BEGIN
  SELECT
    c.component_part_number,
    c.quantity,
    p.quantity_per_skid as component_quantity_per_skid
  FROM volvo_part_components c
  INNER JOIN volvo_masterdata p ON c.component_part_number = p.part_number
  WHERE c.parent_part_number = p_parent_part_number;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartComponent_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartComponent_Insert` (IN `p_parent_part_number` VARCHAR(20), IN `p_component_part_number` VARCHAR(20), IN `p_quantity` INT)   BEGIN
  INSERT INTO volvo_part_components (parent_part_number, component_part_number, quantity)
  VALUES (p_parent_part_number, p_component_part_number, p_quantity);
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartMaster_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartMaster_GetAll` (IN `p_include_inactive` TINYINT(1))   BEGIN
  SELECT part_number, quantity_per_skid, is_active, created_date, modified_date
  FROM volvo_masterdata
  WHERE (p_include_inactive = 1 OR is_active = 1)
  ORDER BY part_number;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartMaster_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartMaster_GetById` (IN `p_part_number` VARCHAR(20))   BEGIN
  SELECT part_number, quantity_per_skid, is_active, created_date, modified_date
  FROM volvo_masterdata
  WHERE part_number = p_part_number;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartMaster_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartMaster_Insert` (IN `p_part_number` VARCHAR(20), IN `p_quantity_per_skid` INT, IN `p_is_active` TINYINT(1))   BEGIN
  INSERT INTO volvo_masterdata (part_number, quantity_per_skid, is_active)
  VALUES (p_part_number, p_quantity_per_skid, p_is_active);
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartMaster_SetActive`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartMaster_SetActive` (IN `p_part_number` VARCHAR(20), IN `p_is_active` TINYINT(1))   BEGIN
  UPDATE volvo_masterdata
  SET
    is_active = p_is_active,
    modified_date = CURRENT_TIMESTAMP
  WHERE part_number = p_part_number;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_PartMaster_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_PartMaster_Update` (IN `p_part_number` VARCHAR(20), IN `p_quantity_per_skid` INT)   BEGIN
  UPDATE volvo_masterdata
  SET
    quantity_per_skid = p_quantity_per_skid,
    modified_date = CURRENT_TIMESTAMP
  WHERE part_number = p_part_number;
END$$

DROP PROCEDURE IF EXISTS `sp_volvo_part_check_references`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_volvo_part_check_references` (IN `p_part_number` VARCHAR(50), OUT `p_active_reference_count` INT)   BEGIN
    
    SELECT COUNT(*) INTO p_active_reference_count
    FROM volvo_line_data vsl
    INNER JOIN volvo_label_data vs ON vsl.shipment_id = vs.id
    WHERE vsl.part_number = p_part_number
      AND vs.status NOT IN ('completed', 'archived')
      AND vs.is_archived = 0;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Settings_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Settings_Get` (IN `p_setting_key` VARCHAR(100))   BEGIN
  SELECT
    setting_key,
    setting_value,
    setting_type,
    category,
    description,
    default_value,
    min_value,
    max_value,
    modified_date,
    modified_by
  FROM settings_module_volvo
  WHERE setting_key = p_setting_key;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Settings_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Settings_GetAll` (IN `p_category` VARCHAR(50))   BEGIN
  IF p_category IS NULL OR p_category = '' THEN
    SELECT
      setting_key,
      setting_value,
      setting_type,
      category,
      description,
      default_value,
      min_value,
      max_value,
      modified_date,
      modified_by
    FROM settings_module_volvo
    ORDER BY category, setting_key;
  ELSE
    SELECT
      setting_key,
      setting_value,
      setting_type,
      category,
      description,
      default_value,
      min_value,
      max_value,
      modified_date,
      modified_by
    FROM settings_module_volvo
    WHERE category = p_category
    ORDER BY setting_key;
  END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Settings_Reset`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Settings_Reset` (IN `p_setting_key` VARCHAR(100), IN `p_modified_by` VARCHAR(50))   BEGIN
  UPDATE settings_module_volvo
  SET setting_value = default_value,
      modified_date = CURRENT_TIMESTAMP,
      modified_by = p_modified_by
  WHERE setting_key = p_setting_key;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Settings_Upsert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Settings_Upsert` (IN `p_setting_key` VARCHAR(100), IN `p_setting_value` TEXT, IN `p_modified_by` VARCHAR(50))   BEGIN
  INSERT INTO settings_module_volvo (setting_key, setting_value, setting_type, category, description, default_value, modified_by)
  SELECT
    p_setting_key,
    p_setting_value,
    setting_type,
    category,
    description,
    default_value,
    p_modified_by
  FROM settings_module_volvo
  WHERE setting_key = p_setting_key
  ON DUPLICATE KEY UPDATE
    setting_value = p_setting_value,
    modified_date = CURRENT_TIMESTAMP,
    modified_by = p_modified_by;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_ShipmentLine_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_ShipmentLine_Delete` (IN `p_id` INT)   BEGIN
  DELETE FROM volvo_line_data
  WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_ShipmentLine_GetByShipment`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_ShipmentLine_GetByShipment` (IN `p_shipment_id` INT)   BEGIN
  SELECT
    id, shipment_id, part_number, location, quantity_per_skid, received_skid_count, calculated_piece_count,
    has_discrepancy, expected_skid_count, discrepancy_note
  FROM volvo_line_data
  WHERE shipment_id = p_shipment_id
  ORDER BY id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_ShipmentLine_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_ShipmentLine_Insert` (IN `p_shipment_id` INT, IN `p_part_number` VARCHAR(20), IN `p_location` VARCHAR(50), IN `p_quantity_per_skid` INT, IN `p_received_skid_count` INT, IN `p_calculated_piece_count` INT, IN `p_has_discrepancy` TINYINT(1), IN `p_expected_skid_count` INT, IN `p_discrepancy_note` TEXT)   BEGIN
  INSERT INTO volvo_line_data (
    shipment_id, part_number, location, quantity_per_skid, received_skid_count, calculated_piece_count,
    has_discrepancy, expected_skid_count, discrepancy_note
  ) VALUES (
    p_shipment_id, p_part_number, NULLIF(TRIM(p_location), ''), p_quantity_per_skid, p_received_skid_count, p_calculated_piece_count,
    p_has_discrepancy, p_expected_skid_count, p_discrepancy_note
  );
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_ShipmentLine_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_ShipmentLine_Update` (IN `p_id` INT, IN `p_location` VARCHAR(50), IN `p_received_skid_count` INT, IN `p_calculated_piece_count` INT, IN `p_has_discrepancy` TINYINT(1), IN `p_expected_skid_count` INT, IN `p_discrepancy_note` TEXT)   BEGIN
  UPDATE volvo_line_data
  SET
    location = NULLIF(TRIM(p_location), ''),
    received_skid_count = p_received_skid_count,
    calculated_piece_count = p_calculated_piece_count,
    has_discrepancy = p_has_discrepancy,
    expected_skid_count = p_expected_skid_count,
    discrepancy_note = p_discrepancy_note
  WHERE id = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Shipment_Complete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Shipment_Complete` (IN `p_shipment_id` INT, IN `p_po_number` VARCHAR(50), IN `p_receiver_number` VARCHAR(50))   BEGIN
  UPDATE volvo_label_data
  SET
    po_number = p_po_number,
    receiver_number = p_receiver_number,
    status = 'completed',
    is_archived = 1,
    modified_date = CURRENT_TIMESTAMP
  WHERE id = p_shipment_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Shipment_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Shipment_Delete` (IN `p_shipment_id` INT)   BEGIN
  
  DELETE FROM volvo_label_data
  WHERE id = p_shipment_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Shipment_GetHistory`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Shipment_GetHistory` (IN `p_start_date` DATE, IN `p_end_date` DATE, IN `p_status` VARCHAR(20))   BEGIN
  SELECT
    s.id, s.shipment_date, s.shipment_number, s.po_number, s.receiver_number,
    s.employee_number, s.notes, s.status, s.created_date, s.modified_date,
    s.is_archived,
    COUNT(l.id) as part_count
  FROM volvo_label_data s
  LEFT JOIN volvo_line_data l ON s.id = l.shipment_id
  WHERE s.shipment_date BETWEEN p_start_date AND p_end_date
    AND (p_status = 'all' OR s.status = p_status)
  GROUP BY s.id
  ORDER BY s.shipment_date DESC, s.shipment_number DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Shipment_GetPending`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Shipment_GetPending` ()   BEGIN
  SELECT
    id, shipment_date, shipment_number, po_number, receiver_number,
    employee_number, notes, status, created_date, modified_date, is_archived
  FROM volvo_label_data
  WHERE status = 'pending_po' AND is_archived = 0
  LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Shipment_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Shipment_Insert` (IN `p_shipment_date` DATE, IN `p_employee_number` VARCHAR(20), IN `p_notes` TEXT, OUT `p_new_id` INT, OUT `p_shipment_number` INT)   BEGIN
  DECLARE v_next_number INT;

  
  SELECT COALESCE(MAX(shipment_number), 0) + 1
  INTO v_next_number
  FROM volvo_label_data
  WHERE shipment_date = p_shipment_date;

  
  INSERT INTO volvo_label_data (
    shipment_date, shipment_number, employee_number, notes, status
  ) VALUES (
    p_shipment_date, v_next_number, p_employee_number, p_notes, 'pending_po'
  );

  SET p_new_id = LAST_INSERT_ID();
  SET p_shipment_number = v_next_number;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_Shipment_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_Shipment_Update` (IN `p_id` INT, IN `p_notes` TEXT)   BEGIN
    UPDATE volvo_label_data
    SET notes = p_notes,
        modified_date = CURRENT_TIMESTAMP
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows;
END$$

DELIMITER ;

DROP TABLE IF EXISTS `auth_users`;
CREATE TABLE `auth_users` (
  `employee_number` int(11) NOT NULL COMMENT 'Unique identifier for each employee',
  `windows_username` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Active Directory username for Windows authentication',
  `full_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee full name for display purposes',
  `pin` varchar(4) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '4-digit PIN for quick authentication at workstations',
  `department` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee department (e.g., Receiving, Shipping, Quality)',
  `shift` enum('1st Shift','2nd Shift','3rd Shift') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee assigned shift',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Flag indicating if employee account is active',
  `visual_username` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Infor Visual ERP username for integration (optional)',
  `visual_password` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Encrypted Infor Visual password (optional)',
  `default_receiving_mode` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Default receiving workflow mode: "guided", "manual", or NULL for always showing selection',
  `default_dunnage_mode` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Default dunnage workflow mode: "guided", "manual", "edit", or NULL for always showing selection',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when user record was created',
  `created_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username of person who created this record',
  `modified_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp of last modification'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Employee authentication and profile data';

TRUNCATE TABLE `auth_users`;
DROP TABLE IF EXISTS `auth_workstation_config`;
CREATE TABLE `auth_workstation_config` (
  `config_id` int(11) NOT NULL COMMENT 'Unique identifier for workstation configuration',
  `workstation_name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Computer name or hostname for workstation identification',
  `workstation_type` enum('shared_terminal','personal_workstation') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Type of workstation: shared_terminal (requires login) or personal_workstation (auto-login)',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Indicates if this workstation configuration is currently active',
  `description` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Optional description of the workstation location or purpose',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when this configuration was created',
  `modified_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp of last modification'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Workstation type detection configuration for authentication flow determination';

TRUNCATE TABLE `auth_workstation_config`;
DROP TABLE IF EXISTS `departments`;
CREATE TABLE `departments` (
  `department_id` int(11) NOT NULL COMMENT 'Unique identifier for department',
  `department_name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Display name of department (must be unique)',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Whether department is available for selection',
  `sort_order` int(11) NOT NULL DEFAULT '999' COMMENT 'Display order in dropdowns (lower = higher)',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when department was created'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Department options for user assignment';

TRUNCATE TABLE `departments`;
INSERT INTO `departments` (`department_id`, `department_name`, `is_active`, `sort_order`, `created_date`) VALUES
(1, 'Receiving', 1, 1, '2026-03-22 16:54:31'),
(2, 'Shipping', 1, 2, '2026-03-22 16:54:31'),
(3, 'Production', 1, 3, '2026-03-22 16:54:31'),
(4, 'Quality Control', 1, 4, '2026-03-22 16:54:31'),
(5, 'Maintenance', 1, 5, '2026-03-22 16:54:31'),
(6, 'Administration', 1, 6, '2026-03-22 16:54:31'),
(7, 'Management', 1, 7, '2026-03-22 16:54:31');

DROP TABLE IF EXISTS `dunnage_custom_fields`;
CREATE TABLE `dunnage_custom_fields` (
  `ID` int(11) NOT NULL COMMENT 'Unique identifier for each custom field',
  `DunnageTypeID` int(11) NOT NULL COMMENT 'References dunnage_types.id',
  `FieldName` varchar(100) NOT NULL COMMENT 'Display name of the field (e.g., "Weight (lbs)")',
  `DatabaseColumnName` varchar(64) NOT NULL COMMENT 'Sanitized column name for database (e.g., "weight_lbs")',
  `FieldType` varchar(20) NOT NULL COMMENT 'Field type: Text, Number, Date, Boolean',
  `DisplayOrder` int(11) NOT NULL COMMENT 'Order of field in UI (1, 2, 3, ...)',
  `IsRequired` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Whether field is mandatory during data entry',
  `ValidationRules` text COMMENT 'JSON validation rules (e.g., {"min": 1, "max": 9999, "decimals": 2})',
  `CreatedDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when field was created',
  `CreatedBy` varchar(50) NOT NULL COMMENT 'Username of person who created the field'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='User-defined custom fields for Add Type Dialog';

TRUNCATE TABLE `dunnage_custom_fields`;
DROP TABLE IF EXISTS `dunnage_history`;
CREATE TABLE `dunnage_history` (
  `load_uuid` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique identifier for the load transaction',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Foreign key to dunnage_parts',
  `quantity` decimal(10,2) NOT NULL COMMENT 'Quantity received in this transaction',
  `received_date` datetime NOT NULL COMMENT 'Date and time the dunnage was received',
  `created_by` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Username of user who created the record',
  `created_date` datetime NOT NULL COMMENT 'Timestamp when record was created',
  `modified_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username of user who last modified the record',
  `modified_date` datetime DEFAULT NULL COMMENT 'Timestamp when record was last modified',
  `po_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'PO number snapshot; NULL for non-PO items',
  `type_id` int(11) DEFAULT NULL COMMENT 'FK to dunnage_types.id; snapshot at time of archive',
  `type_name` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Type name snapshot (e.g. Corrugated Cardboard)',
  `type_icon` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'MaterialIconKind string snapshot (e.g. PackageVariantClosed)',
  `location` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Warehouse location for received dunnage',
  `label_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Label number for this row (supports multi-label splits)',
  `specs_json` json DEFAULT NULL COMMENT 'Snapshot of all per-line dynamic spec key/value pairs as JSON object',
  `archived_at` datetime DEFAULT NULL COMMENT 'Timestamp when the row was moved from the active queue to history',
  `archived_by` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username of the user who triggered Clear Label Data',
  `archive_batch_id` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'UUID shared by all rows archived in the same Clear Label Data operation'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Transaction records of received dunnage items';

TRUNCATE TABLE `dunnage_history`;
DROP TABLE IF EXISTS `dunnage_label_data`;
CREATE TABLE `dunnage_label_data` (
  `id` int(11) NOT NULL COMMENT 'Auto-incrementing unique identifier',
  `load_uuid` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'GUID identifying the load transaction from the workflow session',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part identifier from dunnage_parts',
  `dunnage_type_id` int(11) DEFAULT NULL COMMENT 'FK to dunnage_types.id; snapshot at time of save',
  `dunnage_type_name` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Type name snapshot (e.g. Corrugated Cardboard)',
  `dunnage_type_icon` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'MaterialIconKind string snapshot (e.g. PackageVariantClosed)',
  `quantity` decimal(10,2) NOT NULL COMMENT 'Quantity received in this transaction',
  `po_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'PO number; NULL for non-PO items',
  `received_date` datetime NOT NULL COMMENT 'Date and time the dunnage was received',
  `user_id` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Application user identifier (Windows username)',
  `location` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Warehouse location for received dunnage',
  `label_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Label number for this row (supports multi-label splits)',
  `specs_json` json DEFAULT NULL COMMENT 'Snapshot of all per-line dynamic spec key/value pairs as JSON object',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when the queue record was inserted'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Active dunnage label queue. Rows are moved to dunnage_history on Clear Label Data. Do not write directly to dunnage_history from the workflow.';

TRUNCATE TABLE `dunnage_label_data`;
DROP TABLE IF EXISTS `dunnage_non_po_entries`;
CREATE TABLE `dunnage_non_po_entries` (
  `id` int(10) UNSIGNED NOT NULL,
  `value` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_by` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `use_count` int(10) UNSIGNED NOT NULL DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Saved non-PO reference reasons for dunnage receiving';

TRUNCATE TABLE `dunnage_non_po_entries`;
DROP TABLE IF EXISTS `dunnage_parts`;
CREATE TABLE `dunnage_parts` (
  `id` int(11) NOT NULL COMMENT 'Unique identifier for dunnage part',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Business identifier (e.g., barcode, SKU) for the dunnage part',
  `type_id` int(11) NOT NULL COMMENT 'Foreign key to dunnage_types - defines what kind of dunnage this is',
  `spec_values` json DEFAULT NULL COMMENT 'Dynamic attributes based on type specifications (dimensions, capacity, etc.)',
  `home_location` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Default storage location for this dunnage part',
  `created_by` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'User who created this record',
  `created_date` datetime NOT NULL COMMENT 'Timestamp when record was created',
  `modified_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'User who last modified this record',
  `modified_date` datetime DEFAULT NULL COMMENT 'Timestamp when record was last modified'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Master data for physical dunnage items - links part IDs to types and specifications';

TRUNCATE TABLE `dunnage_parts`;
INSERT INTO `dunnage_parts` (`id`, `part_id`, `type_id`, `spec_values`, `home_location`, `created_by`, `created_date`, `modified_by`, `modified_date`) VALUES
(1, 'Pallet 20x20', 1, '{\"dimensions\": \"20x20\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(2, 'Pallet 32x30', 1, '{\"dimensions\": \"32x30\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(3, 'Pallet 40x48', 1, '{\"dimensions\": \"40x48\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(4, 'Pallet 42x42', 1, '{\"dimensions\": \"42x42\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(5, 'Pallet 42x54', 1, '{\"dimensions\": \"42x54\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(6, 'Pallet 45x48', 1, '{\"dimensions\": \"45x48\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(7, 'Pallet 47x52', 1, '{\"dimensions\": \"47x52\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(8, 'Pallet 48x48', 1, '{\"dimensions\": \"48x48\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(9, 'Pallet 52x47', 1, '{\"dimensions\": \"52x47\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(10, 'Pallet 54x42', 1, '{\"dimensions\": \"54x42\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(11, 'Pallet 74x40', 1, '{\"dimensions\": \"74x40\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(12, 'Pallet 85x40', 1, '{\"dimensions\": \"85x40\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(13, 'Pallet 100x40', 1, '{\"dimensions\": \"100x40\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(14, 'Pallet 40x48 John Deere', 1, '{\"customer\": \"John Deere\", \"dimensions\": \"40x48\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(15, 'Pallet Styberg', 1, '{\"customer\": \"Styberg\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(16, 'Cardboard Sheet 16x38.5', 2, '{\"dimensions\": \"16x38.5\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(17, 'Cardboard Sheet 39x39', 2, '{\"dimensions\": \"39x39\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(18, 'Cardboard Sheet 40x48', 2, '{\"dimensions\": \"40x48\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(19, 'Cardboard Sheet 47x52', 2, '{\"dimensions\": \"47x52\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(20, 'Cardboard Sheet 48x40', 2, '{\"dimensions\": \"48x40\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(21, 'Cardboard Sheet 52x47', 2, '{\"dimensions\": \"52x47\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(22, 'Cardboard Sheet John Deere', 2, '{\"customer\": \"John Deere\"}', 'S-E Racking', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(23, 'Corrugated Box 8x5x5', 3, '{\"width\": \"5\", \"height\": \"5\", \"length\": \"8\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(24, 'Corrugated Box 12x12x6 Single Wall', 3, '{\"width\": \"12\", \"height\": \"6\", \"length\": \"12\", \"wall_type\": \"Single Wall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(25, 'Corrugated Box 12x12x6 Double Wall', 3, '{\"width\": \"12\", \"height\": \"6\", \"length\": \"12\", \"wall_type\": \"Double Wall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(26, 'Corrugated Box 12x12x12 Single Wall', 3, '{\"width\": \"12\", \"height\": \"12\", \"length\": \"12\", \"wall_type\": \"Single Wall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(27, 'Corrugated Box 12x12x12 Double Wall', 3, '{\"width\": \"12\", \"height\": \"12\", \"length\": \"12\", \"wall_type\": \"Double Wall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(28, 'Corrugated Box 16x12x8', 3, '{\"width\": \"12\", \"height\": \"8\", \"length\": \"16\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(29, 'Corrugated Box 24x16x8 Volvo', 3, '{\"width\": \"16\", \"height\": \"8\", \"length\": \"24\", \"customer\": \"Volvo\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(30, 'MPB0000005', 3, '{\"width\": \"16\", \"height\": \"18\", \"length\": \"25\", \"part_number\": \"MPB0000005\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(31, 'Corrugated Box 26x26x26', 3, '{\"width\": \"26\", \"height\": \"26\", \"length\": \"26\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(32, 'Corrugated Box 28x16x17', 3, '{\"width\": \"16\", \"height\": \"17\", \"length\": \"28\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(33, 'Corrugated Box 32x30x15 Electrolux', 3, '{\"width\": \"30\", \"height\": \"15\", \"length\": \"32\", \"customer\": \"Electrolux\"}', 'Floor - In front of V-N Racking', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(34, 'Corrugated Box 32x30x25 Electrolux', 3, '{\"width\": \"30\", \"height\": \"25\", \"length\": \"32\", \"customer\": \"Electrolux\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(35, 'Corrugated Box 42x24x30', 3, '{\"width\": \"24\", \"height\": \"30\", \"length\": \"42\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(36, 'Corrugated Box Lennox', 3, '{\"customer\": \"Lennox\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(37, 'Gaylord Short', 4, '{\"height_type\": \"Short\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(38, 'Gaylord Tall', 4, '{\"height_type\": \"Tall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(39, 'Stretch Film Hand-Held', 5, '{\"application\": \"Hand-Held\"}', 'T - Bay', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(40, 'Stretch Film Auto-Wrapper', 5, '{\"application\": \"Auto-Wrapper\"}', 'Rack by 100-15', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(41, 'Bag 40x48 Lay Flat', 6, '{\"mil\": \"4\", \"style\": \"Lay Flat\", \"width\": \"48\", \"length\": \"40\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(42, 'Bag 40x48 Gaylord', 6, '{\"mil\": \"4\", \"style\": \"Gaylord\", \"width\": \"48\", \"length\": \"40\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(43, 'Tape 2 Inch', 7, '{\"width\": \"2 inch\", \"material\": \"Tape\"}', 'T - Bay', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(44, 'Banding 3/4 Inch Steel', 7, '{\"style\": \"Banding\", \"width\": \"3/4 inch\", \"material\": \"Steel\"}', 'T - Bay', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(45, 'Strapping 1-1/4 Inch Steel', 7, '{\"style\": \"Strapping\", \"width\": \"1-1/4 inch\", \"material\": \"Steel\"}', 'T - Bay', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(46, 'Banding Nylon', 7, '{\"style\": \"Banding\", \"material\": \"Nylon\"}', 'T - Bay', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(47, 'Edge Protector 3x3x6', 8, '{\"depth\": \"3\", \"width\": \"3\", \"length\": \"6\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(48, 'Genfoam 1', 9, '{\"type\": \"Genfoam\", \"variant\": \"1\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(49, 'Genfoam 2', 9, '{\"type\": \"Genfoam\", \"variant\": \"2\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(50, 'Genfoam 3', 9, '{\"type\": \"Genfoam\", \"variant\": \"3\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(51, 'Genfoam 4', 9, '{\"type\": \"Genfoam\", \"variant\": \"4\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(52, 'Gen Walls', 9, '{\"type\": \"Gen Walls\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(53, 'MPB0000017 Cover', 9, '{\"piece\": \"Top / Cover\", \"part_family\": \"MPB0000017\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(54, 'MPB0000017 Box', 9, '{\"piece\": \"Bottom / Box\", \"part_family\": \"MPB0000017\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(55, 'Rack John Deere AKK543', 10, '{\"customer\": \"John Deere\", \"rack_number\": \"AKK543\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(56, 'Rack John Deere AKK46582', 10, '{\"customer\": \"John Deere\", \"rack_number\": \"AKK46582\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(57, 'Rack John Deere AKK46543', 10, '{\"customer\": \"John Deere\", \"rack_number\": \"AKK46543\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(58, 'Rack John Deere AKK546', 10, '{\"customer\": \"John Deere\", \"rack_number\": \"AKK546\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(59, 'Rack John Deere AKK419925', 10, '{\"customer\": \"John Deere\", \"rack_number\": \"AKK419925\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(60, 'Rack Daimler Jackies', 11, '{\"owner\": \"Customer\", \"style\": \"Jackies\", \"customer\": \"Daimler\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(61, 'Rack Crenlo', 11, '{\"owner\": \"Customer\", \"customer\": \"Crenlo\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(62, 'Crate MTM', 11, '{\"owner\": \"MTM\"}', 'Outside Door 5', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(63, 'Tote Kawasaki 14x12x7', 12, '{\"width\": \"12\", \"height\": \"7\", \"length\": \"14\", \"customer\": \"Kawasaki\"}', 'WC', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(64, 'Tote Kawasaki 15x12x7', 12, '{\"width\": \"12\", \"height\": \"7\", \"length\": \"15\", \"customer\": \"Kawasaki\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(65, 'Tote Kawasaki 24x14x7', 12, '{\"width\": \"14\", \"height\": \"7\", \"length\": \"24\", \"customer\": \"Kawasaki\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(66, 'Tote Allison', 12, '{\"customer\": \"Allison\"}', 'RECV', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(67, 'Tote Ariens', 12, '{\"customer\": \"Ariens\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(68, 'Tote Lennox', 12, '{\"customer\": \"Lennox\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(69, 'Tote ZF', 12, '{\"customer\": \"ZF\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(70, 'Basket Insinkerator Short', 13, '{\"customer\": \"Insinkerator\", \"height_type\": \"Short\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(71, 'Basket Insinkerator Tall', 13, '{\"customer\": \"Insinkerator\", \"height_type\": \"Tall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(72, 'Crate Kohler Short', 13, '{\"customer\": \"Kohler\", \"height_type\": \"Short\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(73, 'Crate Kohler Tall', 13, '{\"customer\": \"Kohler\", \"height_type\": \"Tall\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(74, 'Crate Kohler Half', 13, '{\"customer\": \"Kohler\", \"height_type\": \"Half\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL),
(75, 'Jarkies Lennox', 13, '{\"style\": \"Jarkies\", \"customer\": \"Lennox\"}', NULL, 'seed', '2026-03-22 16:55:11', NULL, NULL);

DROP TABLE IF EXISTS `dunnage_requires_inventory`;
CREATE TABLE `dunnage_requires_inventory` (
  `id` int(11) NOT NULL COMMENT 'Unique identifier for inventoried dunnage record',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Reference to dunnage part requiring inventory tracking',
  `inventory_method` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Method used for Visual ERP inventory notification (e.g., manual, automatic)',
  `notes` text COLLATE utf8mb4_unicode_ci COMMENT 'Additional notes about inventory handling or special requirements',
  `created_by` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Username who created the record',
  `created_date` datetime NOT NULL COMMENT 'Timestamp when record was created',
  `modified_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username who last modified the record',
  `modified_date` datetime DEFAULT NULL COMMENT 'Timestamp when record was last modified'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Dunnage parts requiring Visual ERP inventory notification and tracking';

TRUNCATE TABLE `dunnage_requires_inventory`;
DROP TABLE IF EXISTS `dunnage_specs`;
CREATE TABLE `dunnage_specs` (
  `id` int(11) NOT NULL COMMENT 'Primary key for dunnage specifications',
  `type_id` int(11) NOT NULL COMMENT 'Foreign key to dunnage_types table',
  `spec_key` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Specification attribute name (e.g., length, width, material)',
  `spec_value` json DEFAULT NULL COMMENT 'JSON value for the specification (supports flexible data types)',
  `created_by` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Username who created this record',
  `created_date` datetime NOT NULL COMMENT 'Timestamp when record was created',
  `modified_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username who last modified this record',
  `modified_date` datetime DEFAULT NULL COMMENT 'Timestamp when record was last modified'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Dynamic specifications and attributes for each dunnage type';

TRUNCATE TABLE `dunnage_specs`;
DROP TABLE IF EXISTS `dunnage_types`;
CREATE TABLE `dunnage_types` (
  `id` int(11) NOT NULL COMMENT 'Primary key for dunnage type',
  `type_name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Display name of dunnage type (e.g., Box, Pallet, Container)',
  `icon` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'PackageVariantClosed' COMMENT 'MaterialIconKind name for UI display (e.g., PackageVariantClosed, Folder)',
  `created_by` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Username of user who created this record',
  `created_date` datetime NOT NULL COMMENT 'Timestamp when record was created',
  `modified_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username of user who last modified this record',
  `modified_date` datetime DEFAULT NULL COMMENT 'Timestamp when record was last modified'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Type categorization for dunnage items';

TRUNCATE TABLE `dunnage_types`;
INSERT INTO `dunnage_types` (`id`, `type_name`, `icon`, `created_by`, `created_date`, `modified_by`, `modified_date`) VALUES
(1, 'Pallets / Skids', 'ShippingPallet', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(2, 'Cardboard Sheets / Slip Sheets', 'Layers', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(3, 'Corrugated Boxes', 'PackageVariantClosed', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(4, 'Gaylords / Bulk Bins', 'PackageVariant', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(5, 'Stretch Film / Shrink Wrap', 'Autorenew', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(6, 'Bags', 'BagPersonal', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(7, 'Tape / Strapping / Banding', 'Selection', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(8, 'Edge Protectors', 'ShieldOutline', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(9, 'Foam / Molded Inserts', 'LayersOutline', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(10, 'Returnable Racks - John Deere', 'Warehouse', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(11, 'Returnable Racks - Other', 'Forklift', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(12, 'Returnable Totes', 'BoxVariantClosed', 'seed', '2026-03-22 16:55:11', NULL, NULL),
(13, 'Returnable Baskets / Wire Containers', 'BasketOutline', 'seed', '2026-03-22 16:55:11', NULL, NULL);

DROP TABLE IF EXISTS `receiving_history`;
CREATE TABLE `receiving_history` (
  `id` int(11) NOT NULL COMMENT 'Auto-incrementing unique identifier',
  `load_guid` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'GUID identifier for app-generated records; NULL for imported historical records',
  `quantity` int(11) NOT NULL COMMENT 'Quantity of parts received on this label/skid',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part identifier from Infor Visual',
  `po_number` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purchase order number (VARCHAR to support formats like PO-066914 and PO-064489B)',
  `po_line_number` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purchase order line number from Infor Visual',
  `employee_number` int(11) NOT NULL DEFAULT '0' COMMENT 'Employee ID who processed the receiving',
  `heat` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Heat/lot number for material traceability (optional)',
  `transaction_date` date NOT NULL COMMENT 'Date the receiving transaction occurred',
  `initial_location` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Initial warehouse location for received parts (optional)',
  `coils_on_skid` int(11) DEFAULT NULL COMMENT 'Number of coils on the skid (for coil materials, optional)',
  `label_number` int(11) DEFAULT '1' COMMENT 'Sequential label number when splitting quantities (default: 1)',
  `vendor_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Vendor/supplier name (optional, for reference)',
  `part_description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Part description from Infor Visual (optional, for reference)',
  `is_non_po_item` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Non-PO item flag: 1 when part not found in Infor Visual and no PO number',
  `part_skid_sequence` int(11) DEFAULT NULL COMMENT 'Position of this skid among all skids for the same part in the saved batch',
  `part_skid_total` int(11) DEFAULT NULL COMMENT 'Total skids for the same part in the saved batch',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when record was created',
  `PartDescription` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Part description snapshot',
  `POVendor` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'PO vendor snapshot',
  `POStatus` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'PO status snapshot',
  `PODueDate` date DEFAULT NULL COMMENT 'PO due date snapshot',
  `QtyOrdered` decimal(18,2) DEFAULT NULL COMMENT 'Ordered quantity snapshot',
  `UnitOfMeasure` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'UOM snapshot',
  `RemainingQuantity` int(11) DEFAULT NULL COMMENT 'Remaining PO quantity snapshot',
  `UserID` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Windows user / app user id',
  `EmployeeNumber` int(11) DEFAULT NULL COMMENT 'Employee number',
  `IsQualityHoldRequired` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Quality hold required',
  `IsQualityHoldAcknowledged` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Quality hold acknowledged',
  `QualityHoldRestrictionType` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Quality hold restriction type',
  `ArchivedAt` datetime DEFAULT NULL COMMENT 'Archive transfer timestamp',
  `ArchivedBy` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Archive action user',
  `ArchiveBatchID` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Archive operation batch id'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Receiving history - stores all receiving transactions matching receiving_label_data structure';

TRUNCATE TABLE `receiving_history`;
DROP TABLE IF EXISTS `receiving_label_data`;
CREATE TABLE `receiving_label_data` (
  `id` int(11) NOT NULL COMMENT 'Auto-incrementing unique identifier',
  `load_id` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'GUID from the ReceivingLoad session object; NULL until the workflow completes',
  `load_number` int(11) DEFAULT NULL COMMENT 'Sequential load number within the session',
  `quantity` int(11) NOT NULL COMMENT 'Integer quantity of parts received (rounded from weight/qty)',
  `weight_quantity` decimal(18,2) DEFAULT NULL COMMENT 'Raw weight or quantity value entered by the operator',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part identifier from Infor Visual',
  `part_description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Part description sourced from Infor Visual',
  `part_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Part type classification (Standard, Coil, etc.)',
  `po_number` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purchase order number (string-safe for all PO formats)',
  `po_line_number` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'PO line number from Infor Visual',
  `po_vendor` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Vendor name from the PO in Infor Visual',
  `po_status` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'PO status from Infor Visual (Open, Closed, etc.)',
  `po_due_date` date DEFAULT NULL COMMENT 'PO due date from Infor Visual',
  `qty_ordered` decimal(18,2) DEFAULT NULL COMMENT 'Quantity ordered on the PO line',
  `unit_of_measure` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Unit of measure (EA, LB, FT, etc.)',
  `remaining_quantity` int(11) DEFAULT NULL COMMENT 'Remaining open quantity on the PO line',
  `employee_number` int(11) NOT NULL DEFAULT '0' COMMENT 'Employee ID who processed the receiving',
  `user_id` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Application user identifier',
  `heat` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Heat/lot number for material traceability',
  `received_date` datetime DEFAULT NULL COMMENT 'Full timestamp when the record was received',
  `transaction_date` date NOT NULL COMMENT 'Date portion of the receiving transaction',
  `initial_location` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Initial warehouse location for received parts',
  `packages_per_load` int(11) DEFAULT NULL COMMENT 'Number of packages per load/skid',
  `package_type_name` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Package type description (Skid, Box, Coil, etc.)',
  `weight_per_package` decimal(18,2) DEFAULT NULL COMMENT 'Weight of each individual package',
  `coils_on_skid` int(11) DEFAULT NULL COMMENT 'Number of coils on the skid (coil materials only)',
  `label_number` int(11) NOT NULL DEFAULT '1' COMMENT 'Sequential label number when splitting quantities',
  `vendor_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Vendor name (mirrors po_vendor; used for non-PO items)',
  `is_non_po_item` tinyint(1) NOT NULL DEFAULT '0' COMMENT '1 when the part has no PO or was not found in Infor Visual',
  `is_quality_hold_required` tinyint(1) NOT NULL DEFAULT '0' COMMENT '1 when the part requires a quality hold acknowledgment',
  `is_quality_hold_acknowledged` tinyint(1) NOT NULL DEFAULT '0' COMMENT '1 when quality hold has been formally acknowledged',
  `quality_hold_restriction_type` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Restriction type code from the quality hold check',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when record was created',
  `part_skid_sequence` int(11) DEFAULT NULL COMMENT 'Position of this skid among all skids for this part_id in the save batch (e.g. 2)',
  `part_skid_total` int(11) DEFAULT NULL COMMENT 'Total skids for this part_id in the save batch (e.g. 6 → label reads "N of 6")'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Active receiving label queue - rows are moved to receiving_history when the workflow completes (see sp_Receiving_LabelData_ClearToHistory).';

TRUNCATE TABLE `receiving_label_data`;
DROP TABLE IF EXISTS `receiving_package_types`;
CREATE TABLE `receiving_package_types` (
  `PreferenceID` int(11) NOT NULL COMMENT 'Unique identifier for each package type preference',
  `PartID` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part identifier from Infor Visual (unique constraint ensures one preference per part)',
  `PackageTypeName` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Selected package type (Box, Pallet, Custom, etc.)',
  `CustomTypeName` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'User-defined custom package type name when PackageTypeName is "Custom"',
  `LastModified` datetime NOT NULL COMMENT 'Timestamp of last preference update'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User-defined package type preferences mapped to part IDs for receiving workflow';

TRUNCATE TABLE `receiving_package_types`;
DROP TABLE IF EXISTS `receiving_package_type_mapping`;
CREATE TABLE `receiving_package_type_mapping` (
  `id` int(11) NOT NULL COMMENT 'Primary key for mapping record',
  `part_prefix` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part number prefix (e.g., MCC, MMF)',
  `package_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Package type label (e.g., Coils, Sheets, Skids)',
  `is_default` tinyint(1) DEFAULT '0' COMMENT 'If true, used when no prefix matches (fallback)',
  `display_order` int(11) DEFAULT '0' COMMENT 'Sort order for display and selection precedence',
  `is_active` tinyint(1) DEFAULT '1' COMMENT 'Whether this mapping is active (soft-delete flag)',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when record was created',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp when record was last updated',
  `created_by` int(11) DEFAULT NULL COMMENT 'FK toauth_users table (nullable)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Part prefix to package type mappings for receiving workflow';

TRUNCATE TABLE `receiving_package_type_mapping`;
INSERT INTO `receiving_package_type_mapping` (`id`, `part_prefix`, `package_type`, `is_default`, `display_order`, `is_active`, `created_at`, `updated_at`, `created_by`) VALUES
(1, 'MCC', 'Coils', 0, 1, 1, '2026-03-22 16:54:34', '2026-03-22 16:54:34', NULL),
(2, 'MMF', 'Sheets', 0, 2, 1, '2026-03-22 16:54:34', '2026-03-22 16:54:34', NULL),
(3, 'DEFAULT', 'Skids', 1, 99, 1, '2026-03-22 16:54:34', '2026-03-22 16:54:34', NULL);

DROP TABLE IF EXISTS `receiving_quality_holds`;
CREATE TABLE `receiving_quality_holds` (
  `quality_hold_id` int(11) NOT NULL COMMENT 'Unique quality hold identifier',
  `load_id` int(11) NOT NULL COMMENT 'Reference to receiving load',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Restricted part number (e.g., MMFSR05645)',
  `restriction_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Type of restriction (MMFSR, MMCSR)',
  `quality_acknowledged_by` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Quality person who acknowledged the hold',
  `quality_acknowledged_at` datetime DEFAULT NULL COMMENT 'Timestamp when quality acknowledged',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Last update timestamp'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Tracks quality holds for restricted part numbers requiring immediate quality review';

TRUNCATE TABLE `receiving_quality_holds`;
DROP TABLE IF EXISTS `reporting_scheduled_reports`;
CREATE TABLE `reporting_scheduled_reports` (
  `id` int(11) NOT NULL COMMENT 'Primary key: unique scheduled report identifier',
  `report_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Report name/type',
  `schedule` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Schedule string (e.g., Daily at 8:00 AM)',
  `email_recipients` text COLLATE utf8mb4_unicode_ci COMMENT 'Comma-separated email list of recipients',
  `is_active` tinyint(1) DEFAULT '1' COMMENT 'Whether this scheduled report is active/enabled',
  `next_run_date` datetime DEFAULT NULL COMMENT 'Calculated next run time for the schedule',
  `last_run_date` datetime DEFAULT NULL COMMENT 'Last execution timestamp of the report',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Record last updated timestamp',
  `created_by` int(11) DEFAULT NULL COMMENT 'FK toauth_users table who created this schedule'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Scheduled report configurations for reporting module';

TRUNCATE TABLE `reporting_scheduled_reports`;
DROP TABLE IF EXISTS `settings_activity`;
CREATE TABLE `settings_activity` (
  `id` int(11) NOT NULL COMMENT 'Primary key for the audit record',
  `scope` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Setting scope: System or User',
  `category` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Settings category (matches settings_universal.category)',
  `setting_key` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Setting key (matches settings_universal.setting_key)',
  `old_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Previous value before the change',
  `new_value` text COLLATE utf8mb4_unicode_ci COMMENT 'New value after the change',
  `change_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'update' COMMENT 'Nature of the change: create, update, delete, lock, unlock, reset, DefaultApplied',
  `user_id` int(11) DEFAULT NULL COMMENT 'ID of the user who performed the change (0 or NULL for system actions)',
  `changed_by` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Username of the user or system account that made the change',
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'When the change occurred',
  `ip_address` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'IP address of the client performing the change',
  `workstation` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Client workstation or host name where change originated'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Audit trail for all settings changes';

TRUNCATE TABLE `settings_activity`;
INSERT INTO `settings_activity` (`id`, `scope`, `category`, `setting_key`, `old_value`, `new_value`, `change_type`, `user_id`, `changed_by`, `changed_at`, `ip_address`, `workstation`) VALUES
(1, 'System', 'Dunnage', 'Dunnage.Application.DefaultImageLocation', '', '', 'DefaultApplied', 0, 'johnk', '2026-04-23 15:17:25', '', 'JOHNSPC');

DROP TABLE IF EXISTS `settings_dunnage_personal`;
CREATE TABLE `settings_dunnage_personal` (
  `ID` int(11) NOT NULL COMMENT 'Unique identifier for each preference record',
  `UserId` varchar(50) NOT NULL COMMENT 'Windows username or employee number',
  `PreferenceKey` varchar(100) NOT NULL COMMENT 'Preference identifier (e.g., icon_usage_history, pagination_size)',
  `PreferenceValue` text NOT NULL COMMENT 'JSON value for the preference',
  `LastUpdated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp of last update'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Per-user UI preferences for icon picker, pagination, etc.';

TRUNCATE TABLE `settings_dunnage_personal`;
DROP TABLE IF EXISTS `settings_module_volvo`;
CREATE TABLE `settings_module_volvo` (
  `setting_key` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique key identifier for the setting (e.g., AutoPrintEnabled)',
  `setting_value` text COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Stored value as text; interpretation depends on setting_type',
  `setting_type` enum('String','Integer','Boolean','Path','Enum') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Data type used to interpret setting_value',
  `category` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Logical grouping for settings (e.g., Printing, Integration)',
  `description` text COLLATE utf8mb4_unicode_ci COMMENT 'Human-readable description/documentation for the setting',
  `default_value` text COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Default value as text used when no explicit value is set',
  `min_value` int(11) DEFAULT NULL COMMENT 'Minimum allowed numeric value (applies when setting_type = Integer)',
  `max_value` int(11) DEFAULT NULL COMMENT 'Maximum allowed numeric value (applies when setting_type = Integer)',
  `modified_date` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp of last modification',
  `modified_by` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'User identifier who last modified the setting'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Volvo module configurable settings';

TRUNCATE TABLE `settings_module_volvo`;
DROP TABLE IF EXISTS `settings_personal`;
CREATE TABLE `settings_personal` (
  `id` int(11) NOT NULL COMMENT 'Primary key for the settings_personal table',
  `user_id` int(11) NOT NULL COMMENT 'FK to users.id - identifies the user owning this setting',
  `category` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Settings category (matches settings_universal.category)',
  `setting_key` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Setting key (matches settings_universal.setting_key)',
  `setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'User-specific value for this setting',
  `data_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'String' COMMENT 'Data type hint: String, Int, Bool, Json, etc.',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when the record was created',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp when the record was last updated',
  `updated_by` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username of the user who last updated the setting'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User-specific application settings';

TRUNCATE TABLE `settings_personal`;
DROP TABLE IF EXISTS `settings_personal_activity_log`;
CREATE TABLE `settings_personal_activity_log` (
  `log_id` int(11) NOT NULL COMMENT 'Unique identifier for each log entry',
  `event_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Type of event (e.g., Login, Logout, Failed_Login, Password_Change)',
  `username` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username associated with the event (NULL for system events)',
  `workstation_name` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Name of the workstation where the event occurred',
  `event_timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when the event occurred',
  `details` text COLLATE utf8mb4_unicode_ci COMMENT 'Additional JSON or text details about the event'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Audit trail for authentication events and user actions';

TRUNCATE TABLE `settings_personal_activity_log`;
DROP TABLE IF EXISTS `settings_roles`;
CREATE TABLE `settings_roles` (
  `id` int(11) NOT NULL,
  `role_name` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

TRUNCATE TABLE `settings_roles`;
INSERT INTO `settings_roles` (`id`, `role_name`, `description`, `created_at`) VALUES
(1, 'User', 'Basic user role - can modify own settings', '2026-03-22 16:55:05'),
(2, 'Supervisor', 'Supervisor role - can manage user settings', '2026-03-22 16:55:05'),
(3, 'Admin', 'Administrator role - full access to all settings', '2026-03-22 16:55:05'),
(4, 'Developer', 'Developer role - unrestricted access for development', '2026-03-22 16:55:05');

DROP TABLE IF EXISTS `settings_universal`;
CREATE TABLE `settings_universal` (
  `id` int(11) NOT NULL COMMENT 'Auto-incrementing primary key for the setting record',
  `category` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Settings category (System, Security, Receiving, Dunnage, Volvo, Reporting, ERP, UserDefaults)',
  `sub_category` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Optional sub-category for hierarchical organization',
  `setting_key` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique key for the setting (machine identifier, used in code)',
  `setting_name` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Human-readable display name shown in UI (optional)',
  `description` text COLLATE utf8mb4_unicode_ci COMMENT 'Detailed description / help text shown to users or administrators',
  `setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Current value (store JSON for complex types; plain text for simple types)',
  `default_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Factory default value to fall back to when no user value is provided',
  `data_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'String' COMMENT 'Data type hint: String, Int, Bool, Json, Path, Password, Email',
  `scope` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'system' COMMENT 'Determines if the setting is system-wide or can be overridden per user',
  `permission_level` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'admin' COMMENT 'Minimum permission level required to view/modify this setting',
  `is_locked` tinyint(1) DEFAULT '0' COMMENT 'If true, prevents modification even by higher-permission users (administrative lock)',
  `is_sensitive` tinyint(1) DEFAULT '0' COMMENT 'If true, value should be treated as sensitive: encrypted at rest and masked in the UI',
  `validation_rules` json DEFAULT NULL COMMENT 'JSON object containing validation metadata (min, max, pattern, allowed_values, etc.)',
  `ui_control_type` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'textbox' COMMENT 'Preferred UI control type: textbox, numberbox, toggleswitch, combobox, passwordbox, folderpicker, datagrid',
  `ui_order` int(11) DEFAULT '0' COMMENT 'Display order within category (lower values shown first)',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Record last updated timestamp',
  `updated_by` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Username of the user who last updated the setting'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='System-wide application settings';

TRUNCATE TABLE `settings_universal`;
INSERT INTO `settings_universal` (`id`, `category`, `sub_category`, `setting_key`, `setting_name`, `description`, `setting_value`, `default_value`, `data_type`, `scope`, `permission_level`, `is_locked`, `is_sensitive`, `validation_rules`, `ui_control_type`, `ui_order`, `created_at`, `updated_at`, `updated_by`) VALUES
(1, 'Dunnage', NULL, 'Dunnage.Application.DefaultImageLocation', NULL, NULL, '', NULL, 'String', 'system', 'admin', 0, 0, NULL, 'textbox', 0, '2026-04-23 15:17:25', '2026-04-23 15:17:25', 'johnk');

DROP TABLE IF EXISTS `settings_user_roles`;
CREATE TABLE `settings_user_roles` (
  `id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `role_id` int(11) NOT NULL,
  `assigned_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

TRUNCATE TABLE `settings_user_roles`;DROP VIEW IF EXISTS `view_dunnage_history`;
CREATE TABLE `view_dunnage_history` (
`id` char(36)
,`po_number` varchar(20)
,`dunnage_type` varchar(100)
,`part_number` varchar(50)
,`specs_combined` text
,`quantity` decimal(10,2)
,`created_date` date
,`employee_number` varchar(20)
,`created_by_username` varchar(50)
,`location` varchar(100)
,`notes` varchar(255)
,`source_module` varchar(7)
);
DROP VIEW IF EXISTS `view_receiving_history`;
CREATE TABLE `view_receiving_history` (
`id` int(11)
,`po_number` varchar(20)
,`po_line_number` varchar(10)
,`part_number` varchar(50)
,`part_description` varchar(500)
,`quantity` int(11)
,`weight_lbs` decimal(18,2)
,`heat_lot_number` varchar(100)
,`created_date` date
,`employee_number` int(11)
,`load_number` int(11)
,`initial_location` varchar(50)
,`packages_per_load` int(11)
,`package_type_name` varchar(50)
,`coils_on_skid` int(11)
,`label_number` int(11)
,`vendor_name` varchar(255)
,`is_non_po_item` tinyint(4)
,`notes` binary(0)
,`source_module` varchar(9)
);
DROP VIEW IF EXISTS `view_volvo_history`;
CREATE TABLE `view_volvo_history` (
`id` varchar(20)
,`po_number` varchar(50)
,`part_number` varchar(20)
,`quantity` decimal(10,2)
,`created_date` date
,`employee_number` varchar(20)
,`notes` mediumtext
,`shipment_number` int(11)
,`receiver_number` varchar(50)
,`status` varchar(20)
,`part_count` bigint(20)
,`location` varchar(50)
,`quantity_per_skid` int(11)
,`received_skid_count` int(11)
,`source_module` varchar(5)
);
DROP VIEW IF EXISTS `view_volvo_label_data_history`;
CREATE TABLE `view_volvo_label_data_history` (
`shipment_id` int(11)
,`shipment_date` date
,`shipment_number` int(11)
,`po_number` varchar(50)
,`receiver_number` varchar(50)
,`status` varchar(20)
,`part_number` varchar(20)
,`location` varchar(50)
,`received_skid_count` int(11)
,`calculated_piece_count` int(11)
,`has_discrepancy` tinyint(4)
,`expected_skid_count` int(11)
,`discrepancy_note` mediumtext
);

DROP TABLE IF EXISTS `volvo_label_data`;
CREATE TABLE `volvo_label_data` (
  `id` int(11) NOT NULL COMMENT 'Surrogate primary key for shipment record',
  `shipment_date` date NOT NULL COMMENT 'Date of the shipment (local business date)',
  `shipment_number` int(11) NOT NULL COMMENT 'Auto-increment within same day, resets daily; sequence number for shipment_date',
  `po_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purchase order number provided by Purchasing (nullable until PO created)',
  `receiver_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Received package/receiver number populated after Infor Visual receiving',
  `employee_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee identifier from authentication context who created/owns the shipment',
  `notes` text COLLATE utf8mb4_unicode_ci COMMENT 'Optional free-text notes about the shipment',
  `status` enum('pending_po','completed') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'pending_po' COMMENT 'Current lifecycle status of the shipment',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when record was created',
  `modified_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp when record was last modified',
  `is_archived` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Soft-delete flag; 0 = active, 1 = archived'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Volvo dunnage shipment headers';

TRUNCATE TABLE `volvo_label_data`;DROP TRIGGER IF EXISTS `trigger_volvo_shipment_prevent_duplicate_pending`;
DELIMITER $$
CREATE TRIGGER `trigger_volvo_shipment_prevent_duplicate_pending` BEFORE INSERT ON `volvo_label_data` FOR EACH ROW BEGIN
    DECLARE pending_count INT;

    IF NEW.status = 'pending_po'
        AND NEW.is_archived = 0 THEN
        SELECT
            COUNT(*) INTO pending_count
        FROM
            volvo_label_data
        WHERE
            status = 'pending_po'
            AND is_archived = 0;

        IF pending_count > 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Only one pending shipment allowed at a time. Complete existing pending shipment first.';
        END IF;

    END IF;

END
$$
DELIMITER ;
DROP TRIGGER IF EXISTS `trigger_volvo_shipment_prevent_duplicate_pending_update`;
DELIMITER $$
CREATE TRIGGER `trigger_volvo_shipment_prevent_duplicate_pending_update` BEFORE UPDATE ON `volvo_label_data` FOR EACH ROW BEGIN
    DECLARE pending_count INT;

    IF NEW.status = 'pending_po'
        AND NEW.is_archived = 0
        AND (
            OLD.status != 'pending_po'
            OR OLD.is_archived = 1
        ) THEN
        SELECT
            COUNT(*) INTO pending_count
        FROM
            volvo_label_data
        WHERE
            status = 'pending_po'
            AND is_archived = 0
            AND id != NEW.id;

        IF pending_count > 0 THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'Only one pending shipment allowed at a time. Complete existing pending shipment first.';
        END IF;

    END IF;

END
$$
DELIMITER ;

DROP TABLE IF EXISTS `volvo_label_history`;
CREATE TABLE `volvo_label_history` (
  `id` int(11) NOT NULL COMMENT 'Surrogate PK for this history record',
  `original_id` int(11) NOT NULL COMMENT 'Preserved PK from volvo_label_data at time of archive',
  `shipment_date` date NOT NULL COMMENT 'Date of the original shipment (local business date)',
  `shipment_number` int(11) NOT NULL COMMENT 'Auto-increment shipment number (within same day)',
  `po_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purchase order number (populated when shipment was completed)',
  `receiver_number` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Receiver number from Infor Visual',
  `employee_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee identifier who created/owned the shipment',
  `notes` text COLLATE utf8mb4_unicode_ci COMMENT 'Free-text notes about the shipment',
  `status` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Lifecycle status at time of archive (completed)',
  `created_date` datetime NOT NULL COMMENT 'Timestamp when original active record was created',
  `modified_date` datetime NOT NULL COMMENT 'Timestamp of last modification before archive',
  `archived_at` datetime NOT NULL COMMENT 'Timestamp when record was moved to history by Clear Label Data',
  `archived_by` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee number who triggered Clear Label Data',
  `archive_batch_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'UUID shared by all records moved in the same Clear Label Data operation'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Archive of completed Volvo shipment headers cleared from the active queue';

TRUNCATE TABLE `volvo_label_history`;
DROP TABLE IF EXISTS `volvo_line_data`;
CREATE TABLE `volvo_line_data` (
  `id` int(11) NOT NULL COMMENT 'Primary key for volvo_line_data',
  `shipment_id` int(11) NOT NULL COMMENT 'FK to volvo_label_data.id (shipment header)',
  `part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'From volvo_masterdata',
  `location` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Warehouse location for this shipment line',
  `quantity_per_skid` int(11) NOT NULL DEFAULT '0' COMMENT 'Cached quantity per skid from master data at time of shipment',
  `received_skid_count` int(11) NOT NULL COMMENT 'User-entered actual skid count',
  `calculated_piece_count` int(11) NOT NULL COMMENT 'Stored snapshot of calculated piece count from component explosion',
  `has_discrepancy` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Flag indicating mismatch between expected and received counts (0=false,1=true)',
  `expected_skid_count` int(11) DEFAULT NULL COMMENT 'Volvo packlist quantity if discrepancy exists (nullable)',
  `discrepancy_note` text COLLATE utf8mb4_unicode_ci COMMENT 'Optional note describing discrepancy or resolution steps'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Volvo shipment line items';

TRUNCATE TABLE `volvo_line_data`;
DROP TABLE IF EXISTS `volvo_line_history`;
CREATE TABLE `volvo_line_history` (
  `id` int(11) NOT NULL COMMENT 'Surrogate PK for this history record',
  `original_id` int(11) NOT NULL COMMENT 'Preserved PK from volvo_line_data at time of archive',
  `shipment_history_id` int(11) NOT NULL COMMENT 'FK to volvo_label_history.id (archived header)',
  `original_shipment_id` int(11) NOT NULL COMMENT 'Preserved FK from volvo_line_data.shipment_id (original active header ID)',
  `part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Volvo part number (from volvo_masterdata at time of entry)',
  `location` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Warehouse location for this shipment line',
  `quantity_per_skid` int(11) NOT NULL DEFAULT '0' COMMENT 'Cached quantity per skid from master data at time of shipment',
  `received_skid_count` int(11) NOT NULL COMMENT 'User-entered actual skid count',
  `calculated_piece_count` int(11) NOT NULL COMMENT 'Snapshot of calculated piece count',
  `has_discrepancy` tinyint(1) NOT NULL DEFAULT '0' COMMENT '0=no discrepancy, 1=discrepancy noted',
  `expected_skid_count` int(11) DEFAULT NULL COMMENT 'Packlist quantity if discrepancy existed (nullable)',
  `discrepancy_note` text COLLATE utf8mb4_unicode_ci COMMENT 'Note describing discrepancy or resolution',
  `archived_at` datetime NOT NULL COMMENT 'Timestamp when record was moved to history',
  `archived_by` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Employee number who triggered Clear Label Data',
  `archive_batch_id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'UUID shared by all records moved in the same Clear operation'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Archive of Volvo shipment line items cleared from the active queue';

TRUNCATE TABLE `volvo_line_history`;
DROP TABLE IF EXISTS `volvo_masterdata`;
CREATE TABLE `volvo_masterdata` (
  `part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Volvo part identifier (primary key)',
  `quantity_per_skid` int(11) NOT NULL COMMENT 'Pieces per skid for this part (from DataSheet.csv)',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT '0=deactivated, hidden from dropdowns',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `modified_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Record last modified timestamp'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Volvo parts catalog (from DataSheet.csv)';

TRUNCATE TABLE `volvo_masterdata`;
INSERT INTO `volvo_masterdata` (`part_number`, `quantity_per_skid`, `is_active`, `created_date`, `modified_date`) VALUES
('V-EMB-1', 10, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-116', 150, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-2', 20, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-21', 50, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-22', 50, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-26', 32, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-29', 32, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-500', 88, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-6', 10, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-600', 44, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-61', 120, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-62', 120, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-66', 100, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-69', 100, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-701', 10, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-702', 20, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-706', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-71', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-72', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-750', 80, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-757', 80, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-76', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-780', 40, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-787', 40, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-79', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-800', 40, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-840', 20, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-9', 10, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-91', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11'),
('V-EMB-92', 25, 1, '2026-03-22 16:55:11', '2026-03-22 16:55:11');

DROP TABLE IF EXISTS `volvo_part_components`;
CREATE TABLE `volvo_part_components` (
  `id` int(11) NOT NULL COMMENT 'Surrogate primary key for this table',
  `parent_part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Parent part number (references volvo_masterdata.part_number)',
  `component_part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Component part number (references volvo_masterdata.part_number)',
  `quantity` int(11) NOT NULL COMMENT 'How many of this component per parent skid'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Component explosion for Volvo parts';

TRUNCATE TABLE `volvo_part_components`;
INSERT INTO `volvo_part_components` (`id`, `parent_part_number`, `component_part_number`, `quantity`) VALUES
(1, 'V-EMB-116', 'V-EMB-1', 1),
(2, 'V-EMB-116', 'V-EMB-71', 1),
(3, 'V-EMB-116', 'V-EMB-26', 3),
(4, 'V-EMB-21', 'V-EMB-1', 2),
(5, 'V-EMB-22', 'V-EMB-2', 2),
(6, 'V-EMB-500', 'V-EMB-2', 1),
(7, 'V-EMB-500', 'V-EMB-92', 1),
(8, 'V-EMB-600', 'V-EMB-2', 1),
(9, 'V-EMB-600', 'V-EMB-92', 1),
(10, 'V-EMB-61', 'V-EMB-1', 1),
(11, 'V-EMB-61', 'V-EMB-71', 1),
(12, 'V-EMB-61', 'V-EMB-21', 2),
(13, 'V-EMB-62', 'V-EMB-2', 1),
(14, 'V-EMB-62', 'V-EMB-72', 1),
(15, 'V-EMB-62', 'V-EMB-22', 2),
(16, 'V-EMB-66', 'V-EMB-6', 1),
(17, 'V-EMB-66', 'V-EMB-76', 1),
(18, 'V-EMB-66', 'V-EMB-26', 2),
(19, 'V-EMB-69', 'V-EMB-9', 1),
(20, 'V-EMB-69', 'V-EMB-79', 1),
(21, 'V-EMB-69', 'V-EMB-29', 2),
(22, 'V-EMB-702', 'V-EMB-701', 1),
(23, 'V-EMB-706', 'V-EMB-701', 1),
(24, 'V-EMB-71', 'V-EMB-1', 1),
(25, 'V-EMB-72', 'V-EMB-2', 1),
(26, 'V-EMB-750', 'V-EMB-1', 1),
(27, 'V-EMB-750', 'V-EMB-91', 1),
(28, 'V-EMB-76', 'V-EMB-6', 1),
(29, 'V-EMB-780', 'V-EMB-1', 1),
(30, 'V-EMB-780', 'V-EMB-71', 1),
(31, 'V-EMB-79', 'V-EMB-9', 1),
(32, 'V-EMB-800', 'V-EMB-1', 1),
(33, 'V-EMB-800', 'V-EMB-91', 1),
(34, 'V-EMB-91', 'V-EMB-1', 1),
(35, 'V-EMB-92', 'V-EMB-2', 1),
(36, 'V-EMB-26', 'V-EMB-6', 1),
(37, 'V-EMB-29', 'V-EMB-9', 1),
(38, 'V-EMB-787', 'V-EMB-1', 1),
(39, 'V-EMB-787', 'V-EMB-91', 1),
(40, 'V-EMB-757', 'V-EMB-1', 1),
(41, 'V-EMB-757', 'V-EMB-91', 1),
(42, 'V-EMB-840', 'V-EMB-1', 1),
(43, 'V-EMB-840', 'V-EMB-71', 1);


ALTER TABLE `auth_users`
  ADD PRIMARY KEY (`employee_number`),
  ADD UNIQUE KEY `windows_username` (`windows_username`),
  ADD KEY `idx_users_windows_username` (`windows_username`) COMMENT 'Index for fast Windows authentication lookups',
  ADD KEY `idx_users_pin` (`pin`) COMMENT 'Index for fast PIN authentication lookups',
  ADD KEY `idx_users_active` (`is_active`) COMMENT 'Index for filtering active users';

ALTER TABLE `auth_workstation_config`
  ADD PRIMARY KEY (`config_id`),
  ADD UNIQUE KEY `workstation_name` (`workstation_name`),
  ADD KEY `idx_workstation_name` (`workstation_name`) COMMENT 'Index for fast lookup by workstation name',
  ADD KEY `idx_workstation_active` (`is_active`) COMMENT 'Index for filtering active workstations';

ALTER TABLE `departments`
  ADD PRIMARY KEY (`department_id`),
  ADD UNIQUE KEY `department_name` (`department_name`),
  ADD KEY `idx_departments_active` (`is_active`) COMMENT 'Fast filtering of active departments',
  ADD KEY `idx_departments_sort` (`sort_order`) COMMENT 'Optimized sorting for dropdown display';

ALTER TABLE `dunnage_custom_fields`
  ADD PRIMARY KEY (`ID`),
  ADD UNIQUE KEY `IDX_CUSTOM_002` (`DunnageTypeID`,`DatabaseColumnName`) COMMENT 'Prevent duplicate columns per type',
  ADD KEY `IDX_CUSTOM_001` (`DunnageTypeID`) COMMENT 'FK lookup performance';

ALTER TABLE `dunnage_history`
  ADD PRIMARY KEY (`load_uuid`),
  ADD KEY `IDX_LOADS_DATE` (`received_date`) COMMENT 'Edit Mode date range filtering',
  ADD KEY `IDX_LOADS_USER` (`created_by`) COMMENT 'Edit Mode user filtering',
  ADD KEY `FK_dunnage_history_part_id` (`part_id`),
  ADD KEY `IDX_HISTORY_ARCHIVE_BATCH` (`archive_batch_id`),
  ADD KEY `IDX_HISTORY_PO_NUMBER` (`po_number`);

ALTER TABLE `dunnage_label_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_load_uuid` (`load_uuid`) COMMENT 'Lookup by workflow session GUID',
  ADD KEY `idx_part_id` (`part_id`) COMMENT 'Part-based queue queries',
  ADD KEY `idx_received_date` (`received_date`) COMMENT 'Date range filtering for queue consumers';

ALTER TABLE `dunnage_non_po_entries`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_dunnage_non_po_value` (`value`);

ALTER TABLE `dunnage_parts`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `part_id` (`part_id`),
  ADD KEY `FK_dunnage_parts_type_id` (`type_id`);

ALTER TABLE `dunnage_requires_inventory`
  ADD PRIMARY KEY (`id`),
  ADD KEY `FK_dunnage_requires_inventory_part_id` (`part_id`);

ALTER TABLE `dunnage_specs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `UK_dunnage_specs_type_key` (`type_id`,`spec_key`);

ALTER TABLE `dunnage_types`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `type_name` (`type_name`);

ALTER TABLE `receiving_history`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `idx_load_guid` (`load_guid`) COMMENT 'Unique index for app GUID lookups (allows NULLs for imported records)',
  ADD KEY `idx_part_id` (`part_id`) COMMENT 'Index for part lookup queries',
  ADD KEY `idx_po_number` (`po_number`) COMMENT 'Index for PO-based queries',
  ADD KEY `idx_po_line_number` (`po_line_number`) COMMENT 'Index for PO line lookups',
  ADD KEY `idx_transaction_date` (`transaction_date`) COMMENT 'Index for date range queries',
  ADD KEY `idx_employee_number` (`employee_number`) COMMENT 'Index for employee activity queries',
  ADD KEY `idx_po_due_date` (`PODueDate`),
  ADD KEY `idx_archived_at` (`ArchivedAt`),
  ADD KEY `idx_archive_batch` (`ArchiveBatchID`);

ALTER TABLE `receiving_label_data`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_receiving_label_data_load_id` (`load_id`),
  ADD KEY `idx_part_id` (`part_id`) COMMENT 'Index for part lookup queries',
  ADD KEY `idx_po_number` (`po_number`) COMMENT 'Index for PO-based queries',
  ADD KEY `idx_transaction_date` (`transaction_date`) COMMENT 'Index for date range queries',
  ADD KEY `idx_employee_number` (`employee_number`) COMMENT 'Index for employee activity queries',
  ADD KEY `idx_received_date` (`received_date`),
  ADD KEY `idx_user_id` (`user_id`);

ALTER TABLE `receiving_package_types`
  ADD PRIMARY KEY (`PreferenceID`),
  ADD UNIQUE KEY `PartID` (`PartID`),
  ADD KEY `idx_partid` (`PartID`) COMMENT 'Fast lookup index for retrieving preferences by part ID';

ALTER TABLE `receiving_package_type_mapping`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_prefix` (`part_prefix`),
  ADD KEY `idx_active` (`is_active`);

ALTER TABLE `receiving_quality_holds`
  ADD PRIMARY KEY (`quality_hold_id`),
  ADD KEY `idx_load_id` (`load_id`) COMMENT 'Fast lookup by load',
  ADD KEY `idx_part_id` (`part_id`) COMMENT 'Fast lookup by part number',
  ADD KEY `idx_restriction_type` (`restriction_type`) COMMENT 'Filter by restriction type',
  ADD KEY `idx_acknowledged_at` (`quality_acknowledged_at`) COMMENT 'Find unacknowledged holds';

ALTER TABLE `reporting_scheduled_reports`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_next_run` (`next_run_date`),
  ADD KEY `idx_active` (`is_active`),
  ADD KEY `idx_report_type` (`report_type`);

ALTER TABLE `settings_activity`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_category_key` (`category`,`setting_key`),
  ADD KEY `idx_user_id` (`user_id`),
  ADD KEY `idx_changed_by` (`changed_by`),
  ADD KEY `idx_timestamp` (`changed_at`);

ALTER TABLE `settings_dunnage_personal`
  ADD PRIMARY KEY (`ID`),
  ADD UNIQUE KEY `IDX_PREF_001` (`UserId`,`PreferenceKey`) COMMENT 'One value per user per key';

ALTER TABLE `settings_module_volvo`
  ADD PRIMARY KEY (`setting_key`),
  ADD KEY `idx_category` (`category`);

ALTER TABLE `settings_personal`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_user_setting` (`user_id`,`category`,`setting_key`) COMMENT 'Ensures a single override per user per setting',
  ADD KEY `idx_user` (`user_id`) COMMENT 'Index to speed lookups by user_id',
  ADD KEY `idx_category` (`category`);

ALTER TABLE `settings_personal_activity_log`
  ADD PRIMARY KEY (`log_id`),
  ADD KEY `idx_log_timestamp` (`event_timestamp`) COMMENT 'Index for querying logs by timestamp',
  ADD KEY `idx_log_username` (`username`) COMMENT 'Index for querying logs by username',
  ADD KEY `idx_log_event_type` (`event_type`) COMMENT 'Index for querying logs by event type';

ALTER TABLE `settings_roles`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `role_name` (`role_name`);

ALTER TABLE `settings_universal`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_setting` (`category`,`setting_key`),
  ADD KEY `idx_category` (`category`),
  ADD KEY `idx_scope` (`scope`),
  ADD KEY `idx_permission` (`permission_level`);

ALTER TABLE `settings_user_roles`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_settings_user_roles` (`user_id`,`role_id`);

ALTER TABLE `volvo_label_data`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_shipment_per_day` (`shipment_date`,`shipment_number`) COMMENT 'Ensures shipment_number is unique per shipment_date',
  ADD KEY `idx_status` (`status`) COMMENT 'Index to speed queries filtered by status',
  ADD KEY `idx_shipment_date` (`shipment_date`) COMMENT 'Index to speed queries by shipment_date',
  ADD KEY `idx_po_number` (`po_number`) COMMENT 'Index to speed lookups by PO number';

ALTER TABLE `volvo_label_history`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_original_id` (`original_id`) COMMENT 'Look up history by original active-table ID',
  ADD KEY `idx_archive_batch_id` (`archive_batch_id`) COMMENT 'Retrieve all records moved in the same Clear operation',
  ADD KEY `idx_shipment_date` (`shipment_date`) COMMENT 'Filter history by shipment date',
  ADD KEY `idx_employee_number` (`employee_number`) COMMENT 'Filter history by employee';

ALTER TABLE `volvo_line_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_shipment_id` (`shipment_id`) COMMENT 'Index for lookup by shipment_id',
  ADD KEY `idx_part_number` (`part_number`) COMMENT 'Index for lookup by part_number';

ALTER TABLE `volvo_line_history`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_shipment_history_id` (`shipment_history_id`) COMMENT 'Look up lines by archived header ID',
  ADD KEY `idx_original_shipment_id` (`original_shipment_id`) COMMENT 'Look up lines by original active shipment ID',
  ADD KEY `idx_archive_batch_id` (`archive_batch_id`) COMMENT 'Retrieve all records moved in the same Clear operation',
  ADD KEY `idx_part_number` (`part_number`) COMMENT 'Filter history by part number';

ALTER TABLE `volvo_masterdata`
  ADD PRIMARY KEY (`part_number`);

ALTER TABLE `volvo_part_components`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_parent_component` (`parent_part_number`,`component_part_number`) COMMENT 'Ensures each parent/component pair is unique',
  ADD KEY `idx_parent` (`parent_part_number`) COMMENT 'Index to speed parent part lookups',
  ADD KEY `idx_component` (`component_part_number`) COMMENT 'Index to speed component part lookups';


ALTER TABLE `auth_users`
  MODIFY `employee_number` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each employee';

ALTER TABLE `auth_workstation_config`
  MODIFY `config_id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for workstation configuration';

ALTER TABLE `departments`
  MODIFY `department_id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for department', AUTO_INCREMENT=8;

ALTER TABLE `dunnage_custom_fields`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each custom field';

ALTER TABLE `dunnage_label_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Auto-incrementing unique identifier';

ALTER TABLE `dunnage_non_po_entries`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `dunnage_parts`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for dunnage part', AUTO_INCREMENT=76;

ALTER TABLE `dunnage_requires_inventory`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for inventoried dunnage record';

ALTER TABLE `dunnage_specs`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for dunnage specifications';

ALTER TABLE `dunnage_types`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for dunnage type', AUTO_INCREMENT=14;

ALTER TABLE `receiving_history`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Auto-incrementing unique identifier';

ALTER TABLE `receiving_label_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Auto-incrementing unique identifier';

ALTER TABLE `receiving_package_types`
  MODIFY `PreferenceID` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each package type preference';

ALTER TABLE `receiving_package_type_mapping`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for mapping record', AUTO_INCREMENT=4;

ALTER TABLE `receiving_quality_holds`
  MODIFY `quality_hold_id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique quality hold identifier';

ALTER TABLE `reporting_scheduled_reports`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key: unique scheduled report identifier';

ALTER TABLE `settings_activity`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for the audit record', AUTO_INCREMENT=2;

ALTER TABLE `settings_dunnage_personal`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each preference record';

ALTER TABLE `settings_personal`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for the settings_personal table';

ALTER TABLE `settings_personal_activity_log`
  MODIFY `log_id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each log entry';

ALTER TABLE `settings_roles`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

ALTER TABLE `settings_universal`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Auto-incrementing primary key for the setting record', AUTO_INCREMENT=2;

ALTER TABLE `settings_user_roles`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

ALTER TABLE `volvo_label_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key for shipment record';

ALTER TABLE `volvo_label_history`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate PK for this history record';

ALTER TABLE `volvo_line_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for volvo_line_data';

ALTER TABLE `volvo_line_history`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate PK for this history record';

ALTER TABLE `volvo_part_components`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key for this table', AUTO_INCREMENT=44;
DROP TABLE IF EXISTS `view_dunnage_history`;

DROP VIEW IF EXISTS `view_dunnage_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_dunnage_history`  AS SELECT `dl`.`load_uuid` AS `id`, cast(NULL as char(20) charset utf8mb4) AS `po_number`, `dt`.`type_name` AS `dunnage_type`, `dp`.`part_id` AS `part_number`, group_concat(concat(`ds`.`spec_key`,':',coalesce(json_unquote(`ds`.`spec_value`),'')) order by `ds`.`spec_key` ASC separator ', ') AS `specs_combined`, `dl`.`quantity` AS `quantity`, cast(`dl`.`received_date` as date) AS `created_date`, cast(`au`.`employee_number` as char(20) charset utf8mb4) AS `employee_number`, `dl`.`created_by` AS `created_by_username`, `dp`.`home_location` AS `location`, cast(NULL as char(255) charset utf8mb4) AS `notes`, 'Dunnage' AS `source_module` FROM ((((`dunnage_history` `dl` join `dunnage_parts` `dp` on((`dl`.`part_id` = `dp`.`part_id`))) join `dunnage_types` `dt` on((`dp`.`type_id` = `dt`.`id`))) left join `dunnage_specs` `ds` on((`dp`.`type_id` = `ds`.`type_id`))) left join `auth_users` `au` on(((`au`.`windows_username` = `dl`.`created_by`) and (`au`.`is_active` = TRUE)))) GROUP BY `dl`.`load_uuid`, `dt`.`type_name`, `dp`.`part_id`, `dl`.`quantity`, `dl`.`received_date`, `dl`.`created_by`, `dp`.`home_location`, `au`.`employee_number` ORDER BY `dl`.`received_date` DESC ;
DROP TABLE IF EXISTS `view_receiving_history`;

DROP VIEW IF EXISTS `view_receiving_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_receiving_history`  AS SELECT `receiving_history`.`id` AS `id`, `receiving_history`.`po_number` AS `po_number`, `receiving_history`.`po_line_number` AS `po_line_number`, `receiving_history`.`part_id` AS `part_number`, `receiving_history`.`part_description` AS `part_description`, `receiving_history`.`quantity` AS `quantity`, cast(NULL as decimal(18,2)) AS `weight_lbs`, `receiving_history`.`heat` AS `heat_lot_number`, cast(coalesce(`receiving_history`.`transaction_date`,`receiving_history`.`created_at`) as date) AS `created_date`, `receiving_history`.`employee_number` AS `employee_number`, NULL AS `load_number`, `receiving_history`.`initial_location` AS `initial_location`, NULL AS `packages_per_load`, NULL AS `package_type_name`, `receiving_history`.`coils_on_skid` AS `coils_on_skid`, `receiving_history`.`label_number` AS `label_number`, `receiving_history`.`vendor_name` AS `vendor_name`, `receiving_history`.`is_non_po_item` AS `is_non_po_item`, NULL AS `notes`, 'Receiving' AS `source_module` FROM `receiving_history`union all select `receiving_label_data`.`id` AS `id`,`receiving_label_data`.`po_number` AS `po_number`,`receiving_label_data`.`po_line_number` AS `po_line_number`,`receiving_label_data`.`part_id` AS `part_number`,`receiving_label_data`.`part_description` AS `part_description`,`receiving_label_data`.`quantity` AS `quantity`,`receiving_label_data`.`weight_quantity` AS `weight_lbs`,`receiving_label_data`.`heat` AS `heat_lot_number`,cast(coalesce(`receiving_label_data`.`transaction_date`,`receiving_label_data`.`created_at`) as date) AS `created_date`,`receiving_label_data`.`employee_number` AS `employee_number`,`receiving_label_data`.`load_number` AS `load_number`,`receiving_label_data`.`initial_location` AS `initial_location`,`receiving_label_data`.`packages_per_load` AS `packages_per_load`,`receiving_label_data`.`package_type_name` AS `package_type_name`,`receiving_label_data`.`coils_on_skid` AS `coils_on_skid`,`receiving_label_data`.`label_number` AS `label_number`,`receiving_label_data`.`vendor_name` AS `vendor_name`,`receiving_label_data`.`is_non_po_item` AS `is_non_po_item`,NULL AS `notes`,'Receiving' AS `source_module` from `receiving_label_data` order by `created_date` desc,`id` desc  ;
DROP TABLE IF EXISTS `view_volvo_history`;

DROP VIEW IF EXISTS `view_volvo_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_volvo_history`  AS SELECT cast(`vsl`.`id` as char(20) charset utf8mb4) AS `id`, `vs`.`po_number` AS `po_number`, `vsl`.`part_number` AS `part_number`, cast(`vsl`.`calculated_piece_count` as decimal(10,2)) AS `quantity`, cast(`vs`.`created_date` as date) AS `created_date`, `vs`.`employee_number` AS `employee_number`, coalesce(`vsl`.`discrepancy_note`,`vs`.`notes`) AS `notes`, `vs`.`shipment_number` AS `shipment_number`, `vs`.`receiver_number` AS `receiver_number`, `vs`.`status` AS `status`, 1 AS `part_count`, `vsl`.`location` AS `location`, `vsl`.`quantity_per_skid` AS `quantity_per_skid`, `vsl`.`received_skid_count` AS `received_skid_count`, 'Volvo' AS `source_module` FROM (`volvo_label_data` `vs` join `volvo_line_data` `vsl` on((`vs`.`id` = `vsl`.`shipment_id`)))union all select cast(`vlnh`.`id` as char(20) charset utf8mb4) AS `id`,`vlh`.`po_number` AS `po_number`,`vlnh`.`part_number` AS `part_number`,cast(`vlnh`.`calculated_piece_count` as decimal(10,2)) AS `quantity`,cast(`vlh`.`created_date` as date) AS `created_date`,`vlh`.`employee_number` AS `employee_number`,coalesce(`vlnh`.`discrepancy_note`,`vlh`.`notes`) AS `notes`,`vlh`.`shipment_number` AS `shipment_number`,`vlh`.`receiver_number` AS `receiver_number`,`vlh`.`status` AS `status`,1 AS `part_count`,`vlnh`.`location` AS `location`,`vlnh`.`quantity_per_skid` AS `quantity_per_skid`,`vlnh`.`received_skid_count` AS `received_skid_count`,'Volvo' AS `source_module` from (`volvo_label_history` `vlh` join `volvo_line_history` `vlnh` on((`vlh`.`id` = `vlnh`.`shipment_history_id`))) order by `created_date` desc  ;
DROP TABLE IF EXISTS `view_volvo_label_data_history`;

DROP VIEW IF EXISTS `view_volvo_label_data_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_volvo_label_data_history`  AS SELECT `s`.`id` AS `shipment_id`, `s`.`shipment_date` AS `shipment_date`, `s`.`shipment_number` AS `shipment_number`, `s`.`po_number` AS `po_number`, `s`.`receiver_number` AS `receiver_number`, `s`.`status` AS `status`, `l`.`part_number` AS `part_number`, `l`.`location` AS `location`, `l`.`received_skid_count` AS `received_skid_count`, `l`.`calculated_piece_count` AS `calculated_piece_count`, `l`.`has_discrepancy` AS `has_discrepancy`, `l`.`expected_skid_count` AS `expected_skid_count`, `l`.`discrepancy_note` AS `discrepancy_note` FROM (`volvo_label_data` `s` left join `volvo_line_data` `l` on((`s`.`id` = `l`.`shipment_id`)))union all select `vlh`.`id` AS `shipment_id`,`vlh`.`shipment_date` AS `shipment_date`,`vlh`.`shipment_number` AS `shipment_number`,`vlh`.`po_number` AS `po_number`,`vlh`.`receiver_number` AS `receiver_number`,`vlh`.`status` AS `status`,`vlnh`.`part_number` AS `part_number`,`vlnh`.`location` AS `location`,`vlnh`.`received_skid_count` AS `received_skid_count`,`vlnh`.`calculated_piece_count` AS `calculated_piece_count`,`vlnh`.`has_discrepancy` AS `has_discrepancy`,`vlnh`.`expected_skid_count` AS `expected_skid_count`,`vlnh`.`discrepancy_note` AS `discrepancy_note` from (`volvo_label_history` `vlh` left join `volvo_line_history` `vlnh` on((`vlh`.`id` = `vlnh`.`shipment_history_id`))) order by `shipment_date` desc,`shipment_number` desc  ;


ALTER TABLE `dunnage_custom_fields`
  ADD CONSTRAINT `dunnage_custom_fields_ibfk_1` FOREIGN KEY (`DunnageTypeID`) REFERENCES `dunnage_types` (`id`) ON DELETE CASCADE;

ALTER TABLE `dunnage_history`
  ADD CONSTRAINT `FK_dunnage_history_part_id` FOREIGN KEY (`part_id`) REFERENCES `dunnage_parts` (`part_id`);

ALTER TABLE `dunnage_parts`
  ADD CONSTRAINT `FK_dunnage_parts_type_id` FOREIGN KEY (`type_id`) REFERENCES `dunnage_types` (`id`);

ALTER TABLE `dunnage_requires_inventory`
  ADD CONSTRAINT `FK_dunnage_requires_inventory_part_id` FOREIGN KEY (`part_id`) REFERENCES `dunnage_parts` (`part_id`) ON DELETE CASCADE;

ALTER TABLE `dunnage_specs`
  ADD CONSTRAINT `FK_dunnage_specs_type_id` FOREIGN KEY (`type_id`) REFERENCES `dunnage_types` (`id`) ON DELETE CASCADE;

ALTER TABLE `receiving_quality_holds`
  ADD CONSTRAINT `fk_quality_holds_load` FOREIGN KEY (`load_id`) REFERENCES `receiving_loads` (`load_id`) ON DELETE CASCADE ON UPDATE CASCADE;

ALTER TABLE `volvo_line_data`
  ADD CONSTRAINT `volvo_line_data_ibfk_1` FOREIGN KEY (`shipment_id`) REFERENCES `volvo_label_data` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `volvo_line_data_ibfk_2` FOREIGN KEY (`part_number`) REFERENCES `volvo_masterdata` (`part_number`);

ALTER TABLE `volvo_line_history`
  ADD CONSTRAINT `fk_volvo_line_history_header` FOREIGN KEY (`shipment_history_id`) REFERENCES `volvo_label_history` (`id`) ON DELETE CASCADE;

ALTER TABLE `volvo_part_components`
  ADD CONSTRAINT `fk_volvo_part_components_component` FOREIGN KEY (`component_part_number`) REFERENCES `volvo_masterdata` (`part_number`),
  ADD CONSTRAINT `fk_volvo_part_components_parent` FOREIGN KEY (`parent_part_number`) REFERENCES `volvo_masterdata` (`part_number`) ON DELETE CASCADE;

SET FOREIGN_KEY_CHECKS=1;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
