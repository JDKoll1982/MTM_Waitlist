SET FOREIGN_KEY_CHECKS=0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

DROP DATABASE IF EXISTS `mtm_receiving_application_test`;
CREATE DATABASE IF NOT EXISTS `mtm_receiving_application_test` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `mtm_receiving_application_test`;

DELIMITER $$
DROP PROCEDURE IF EXISTS `sp_Auth_User_GetDefaultMode`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_GetDefaultMode` (IN `p_user_id` INT)   BEGIN
    

    SELECT default_receiving_mode
    FROM auth_users
    WHERE employee_number = p_user_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Auth_User_UpdateDefaultMode`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Auth_User_UpdateDefaultMode` (IN `p_user_id` INT, IN `p_default_mode` VARCHAR(20))   BEGIN
    
    

    UPDATE auth_users
    SET default_receiving_mode = p_default_mode
    WHERE employee_number = p_user_id;

    SELECT ROW_COUNT() AS affected_rows;
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

DROP PROCEDURE IF EXISTS `sp_Dunnage_Line_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Line_Insert` (IN `p_Line1` VARCHAR(255), IN `p_Line2` VARCHAR(255), IN `p_PONumber` INT, IN `p_Date` DATE, IN `p_EmployeeNumber` INT, IN `p_VendorName` VARCHAR(255), IN `p_Location` VARCHAR(50), OUT `p_Status` INT, OUT `p_ErrorMsg` VARCHAR(500))   BEGIN
    
    
    SET p_Status = 1;
    SET p_ErrorMsg = 'sp_Dunnage_Line_Insert is deprecated - use dunnage_history table';
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

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_Insert` (IN `p_load_uuid` CHAR(36), IN `p_part_id` VARCHAR(50), IN `p_quantity` DECIMAL(10,2), IN `p_user` VARCHAR(50))   BEGIN
    INSERT INTO dunnage_history (
        load_uuid,
        part_id,
        quantity,
        received_date,
        created_by,
        created_date
    ) VALUES (
        p_load_uuid,
        p_part_id,
        p_quantity,
        NOW(),
        p_user,
        NOW()
    );
END$$

DROP PROCEDURE IF EXISTS `sp_Dunnage_Loads_InsertBatch`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Loads_InsertBatch` (IN `p_load_data` JSON, IN `p_user` VARCHAR(50))   BEGIN
    DECLARE i INT DEFAULT 0;
    DECLARE count INT DEFAULT 0;
    DECLARE v_load_uuid CHAR(36);
    DECLARE v_part_id VARCHAR(50);
    DECLARE v_quantity DECIMAL(10,2);

    SET count = JSON_LENGTH(p_load_data);

    WHILE i < count DO
        SET v_load_uuid = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].load_uuid')));
        SET v_part_id = JSON_UNQUOTE(JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].part_id')));
        SET v_quantity = JSON_EXTRACT(p_load_data, CONCAT('$[', i, '].quantity'));

        INSERT INTO dunnage_history (
            load_uuid,
            part_id,
            quantity,
            received_date,
            created_by,
            created_date
        ) VALUES (
            v_load_uuid,
            v_part_id,
            v_quantity,
            NOW(),
            p_user,
            NOW()
        );

        SET i = i + 1;
    END WHILE;
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
        p.part_id LIKE CONCAT('%', p_search_text, '%')
        OR JSON_SEARCH(p.spec_values, 'one', CONCAT('%', p_search_text, '%')) IS NOT NULL
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
        SpecKey VARCHAR(100)
    );

    TRUNCATE TABLE temp_spec_keys;

    INSERT INTO temp_spec_keys (SpecKey)
    SELECT DISTINCT
        JSON_UNQUOTE(JSON_EXTRACT(JSON_KEYS(ds.spec_value), CONCAT('$[', nums.n - 1, ']'))) AS SpecKey
    FROM dunnage_specs ds
    JOIN (
        SELECT 1 n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
        UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10
        UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
        UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
        UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL SELECT 25
        UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30
        UNION ALL SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 33 UNION ALL SELECT 34 UNION ALL SELECT 35
        UNION ALL SELECT 36 UNION ALL SELECT 37 UNION ALL SELECT 38 UNION ALL SELECT 39 UNION ALL SELECT 40
        UNION ALL SELECT 41 UNION ALL SELECT 42 UNION ALL SELECT 43 UNION ALL SELECT 44 UNION ALL SELECT 45
        UNION ALL SELECT 46 UNION ALL SELECT 47 UNION ALL SELECT 48 UNION ALL SELECT 49 UNION ALL SELECT 50
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
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_Types_Insert` (IN `p_type_name` VARCHAR(100), IN `p_icon` VARCHAR(50), IN `p_created_by` VARCHAR(50))   BEGIN
    
    IF EXISTS (SELECT 1 FROM dunnage_types WHERE type_name = p_type_name) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Dunnage type name already exists';
    END IF;

    INSERT INTO dunnage_types (type_name, icon, created_by, created_date)
    VALUES (p_type_name, p_icon, p_created_by, NOW());

    SELECT LAST_INSERT_ID() AS id;
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
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Dunnage_UserPreferences_Upsert` (IN `p_user_id` VARCHAR(50), IN `p_preference_key` VARCHAR(100), IN `p_preference_value` TEXT, OUT `p_status` INT, OUT `p_error_msg` VARCHAR(500))   BEGIN
    
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1 p_error_msg = MESSAGE_TEXT;
        SET p_status = -1;
        ROLLBACK;
    END;

    
    START TRANSACTION;

    
    INSERT INTO settings_dunnage_personal (UserId, PreferenceKey, PreferenceValue, LastUpdated)
    VALUES (p_user_id, p_preference_key, p_preference_value, NOW())
    ON DUPLICATE KEY UPDATE
        PreferenceValue = VALUES(PreferenceValue),
        LastUpdated = NOW();

    
    SET p_status = 1;
    SET p_error_msg = 'User preference saved successfully';
    COMMIT;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_History_Get`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_History_Get` (IN `p_PartID` VARCHAR(50), IN `p_StartDate` DATETIME, IN `p_EndDate` DATETIME)   BEGIN
    SELECT * FROM receiving_history
    WHERE PartID = p_PartID
    AND ReceivedDate BETWEEN p_StartDate AND p_EndDate
    ORDER BY ReceivedDate DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Line_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Line_Insert` (IN `p_Quantity` INT, IN `p_PartID` VARCHAR(50), IN `p_PONumber` INT, IN `p_EmployeeNumber` INT, IN `p_Heat` VARCHAR(100), IN `p_Date` DATE, IN `p_InitialLocation` VARCHAR(50), IN `p_CoilsOnSkid` INT, IN `p_VendorName` VARCHAR(255), IN `p_PartDescription` VARCHAR(500), OUT `p_Status` INT, OUT `p_ErrorMsg` VARCHAR(500))   BEGIN
    
    SET p_Status = 1;
    SET p_ErrorMsg = 'sp_Receiving_Line_Insert is deprecated - use receiving_history table';
END$$

DROP PROCEDURE IF EXISTS `sp_Receiving_Load_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Receiving_Load_Insert` (IN `p_LoadID` VARCHAR(36), IN `p_PartID` VARCHAR(50), IN `p_PartType` VARCHAR(20), IN `p_PONumber` VARCHAR(20), IN `p_POLineNumber` VARCHAR(10), IN `p_LoadNumber` INT, IN `p_WeightQuantity` DECIMAL(18,2), IN `p_HeatLotNumber` VARCHAR(50), IN `p_PackagesPerLoad` INT, IN `p_PackageTypeName` VARCHAR(50), IN `p_WeightPerPackage` DECIMAL(18,2), IN `p_IsNonPOItem` BIT, IN `p_ReceivedDate` DATETIME)   BEGIN
    INSERT INTO receiving_history
    (LoadID, PartID, PartType, PONumber, POLineNumber, LoadNumber,
     WeightQuantity, HeatLotNumber, PackagesPerLoad, PackageTypeName,
     WeightPerPackage, IsNonPOItem, ReceivedDate)
    VALUES
    (p_LoadID, p_PartID, p_PartType, p_PONumber, p_POLineNumber, p_LoadNumber,
     p_WeightQuantity, p_HeatLotNumber, p_PackagesPerLoad, p_PackageTypeName,
     p_WeightPerPackage, p_IsNonPOItem, p_ReceivedDate);
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

END sp_end$$

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

DROP PROCEDURE IF EXISTS `sp_routing_recipient_get_all`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_routing_recipient_get_all` ()   BEGIN
    
    SELECT
        `id`,
        `name`,
        `location`,
        `department`,
        `is_active`,
        `created_date`,
        `updated_date`
    FROM `routing_recipients`
    WHERE `is_active` = 1
    ORDER BY `name` ASC;
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

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_Delete`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_Delete` (IN `p_id` INT)   BEGIN
    DECLARE v_exists INT DEFAULT 0;
    DECLARE v_affected INT DEFAULT 0;

    
    SELECT COUNT(1) INTO v_exists
    FROM routing_home_locations
    WHERE id = p_id;

    IF v_exists = 0 THEN
        
        SELECT 0 AS affected_rows, 'NotFound' AS status;
    ELSE
        
        UPDATE routing_home_locations
        SET is_active = 0,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = p_id AND is_active = 1;

        SET v_affected = ROW_COUNT();

        SELECT v_affected AS affected_rows,
               CASE
                   WHEN v_affected = 1 THEN 'Deleted'
                   ELSE 'AlreadyInactive'
               END AS status;
    END IF;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_FindMatch`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_FindMatch` (IN `p_match_type` VARCHAR(50), IN `p_value` VARCHAR(100))   BEGIN
    SELECT
        r.id,
        r.match_type,
        r.`pattern`,
        r.destination_location,
        r.priority
    FROM routing_home_locations AS r
    WHERE r.match_type = p_match_type
      AND r.is_active = 1
      AND (
          p_value LIKE REPLACE(REPLACE(r.`pattern`, '*', '%'), '?', '_')
          OR r.`pattern` = p_value
      )
    ORDER BY r.priority ASC
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_GetAll`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_GetAll` ()   BEGIN
    SELECT
        `id`,
        `match_type`,
        `pattern`,
        `destination_location`,
        `priority`,
        `is_active`,
        `created_at`,
        `updated_at`,
        `created_by`
    FROM `routing_home_locations`
    WHERE `is_active` = 1
    ORDER BY `priority` ASC, `created_at` DESC;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_GetById`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_GetById` (IN `p_id` INT)   BEGIN
    SELECT
        `id`,
        `match_type`,
        `pattern`,
        `destination_location`,
        `priority`,
        `is_active`,
        `created_at`,
        `updated_at`,
        `created_by`
    FROM `routing_home_locations`
    WHERE `id` = p_id;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_GetByPartNumber`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_GetByPartNumber` (IN `p_part_number` VARCHAR(100))   BEGIN
    SELECT
        id,
        match_type,
        pattern,
        destination_location,
        priority
    FROM routing_home_locations
    WHERE match_type = 'Part Number'
      AND is_active = 1
      AND (
          p_part_number LIKE REPLACE(REPLACE(pattern, '*', '%'), '?', '_')
          OR pattern = p_part_number
      )
    ORDER BY priority ASC
    LIMIT 1;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_Insert` (IN `p_match_type` VARCHAR(50), IN `p_pattern` VARCHAR(100), IN `p_destination_location` VARCHAR(50), IN `p_priority` INT, IN `p_created_by` INT)   BEGIN
    
    IF EXISTS (
        SELECT 1
        FROM `routing_home_locations`
        WHERE `match_type` = p_match_type
          AND `pattern` = p_pattern
          AND `is_active` = 1
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Routing rule with this match type and pattern already exists';
    END IF;

    
    INSERT INTO `routing_home_locations`
        (`match_type`, `pattern`, `destination_location`, `priority`, `is_active`, `created_by`)
    VALUES
        (p_match_type, p_pattern, p_destination_location, COALESCE(p_priority, 50), 1, p_created_by);

    SELECT LAST_INSERT_ID() AS `id`;
END$$

DROP PROCEDURE IF EXISTS `sp_Settings_RoutingRule_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Settings_RoutingRule_Update` (IN `p_id` INT, IN `p_match_type` VARCHAR(50), IN `p_pattern` VARCHAR(100), IN `p_destination_location` VARCHAR(50), IN `p_priority` INT)   BEGIN
    
    
    
    IF EXISTS (SELECT 1 FROM routing_home_locations WHERE match_type = p_match_type AND pattern = p_pattern AND id != p_id) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Routing rule with this match type and pattern already exists';
    END IF;

    UPDATE routing_home_locations
    SET match_type = p_match_type,
        pattern = p_pattern,
        destination_location = p_destination_location,
        priority = p_priority,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_id;

    SELECT ROW_COUNT() AS affected_rows;
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
    id, shipment_id, part_number, quantity_per_skid, received_skid_count, calculated_piece_count,
    has_discrepancy, expected_skid_count, discrepancy_note
  FROM volvo_line_data
  WHERE shipment_id = p_shipment_id
  ORDER BY id;
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_ShipmentLine_Insert`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_ShipmentLine_Insert` (IN `p_shipment_id` INT, IN `p_part_number` VARCHAR(20), IN `p_quantity_per_skid` INT, IN `p_received_skid_count` INT, IN `p_calculated_piece_count` INT, IN `p_has_discrepancy` TINYINT(1), IN `p_expected_skid_count` INT, IN `p_discrepancy_note` TEXT)   BEGIN
  INSERT INTO volvo_line_data (
    shipment_id, part_number, quantity_per_skid, received_skid_count, calculated_piece_count,
    has_discrepancy, expected_skid_count, discrepancy_note
  ) VALUES (
    p_shipment_id, p_part_number, p_quantity_per_skid, p_received_skid_count, p_calculated_piece_count,
    p_has_discrepancy, p_expected_skid_count, p_discrepancy_note
  );
END$$

DROP PROCEDURE IF EXISTS `sp_Volvo_ShipmentLine_Update`$$
CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_Volvo_ShipmentLine_Update` (IN `p_id` INT, IN `p_received_skid_count` INT, IN `p_calculated_piece_count` INT, IN `p_has_discrepancy` TINYINT(1), IN `p_expected_skid_count` INT, IN `p_discrepancy_note` TEXT)   BEGIN
  UPDATE volvo_line_data
  SET
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
(1, 'Receiving', 1, 1, '2026-01-12 08:42:37'),
(2, 'Shipping', 1, 2, '2026-01-12 08:42:37'),
(3, 'Production', 1, 3, '2026-01-12 08:42:37'),
(4, 'Quality Control', 1, 4, '2026-01-12 08:42:37'),
(5, 'Maintenance', 1, 5, '2026-01-12 08:42:37'),
(6, 'Administration', 1, 6, '2026-01-12 08:42:37'),
(7, 'Management', 1, 7, '2026-01-12 08:42:37');

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
  `modified_date` datetime DEFAULT NULL COMMENT 'Timestamp when record was last modified'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Transaction records of received dunnage items';

TRUNCATE TABLE `dunnage_history`;
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
DROP TABLE IF EXISTS `receiving_history`;
CREATE TABLE `receiving_history` (
  `LoadID` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'GUID (UUID) identifier for the receiving load',
  `PartID` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Internal part identifier (FK to parts table)',
  `PartType` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part classification or category',
  `PONumber` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Purchase Order number (nullable for non-PO items)',
  `POLineNumber` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Purchase Order line number',
  `LoadNumber` int(11) NOT NULL COMMENT 'Sequential load number assigned by receiving',
  `WeightQuantity` decimal(10,2) NOT NULL COMMENT 'Total weight for the entire load (units: configured weight unit)',
  `HeatLotNumber` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Heat or lot tracking number for traceability',
  `PackagesPerLoad` int(11) NOT NULL COMMENT 'Number of packages contained in this load',
  `PackageTypeName` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Human-readable package type name (e.g., pallet, skid, coil)',
  `WeightPerPackage` decimal(10,2) NOT NULL COMMENT 'Calculated or recorded weight per package (units: configured weight unit)',
  `IsNonPOItem` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Flag indicating this item was received without a purchase order (1 = non-PO)',
  `ReceivedDate` datetime NOT NULL COMMENT 'Date and time when the load was received by warehouse',
  `CreatedAt` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp (automatically set)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Receiving loads (skids) table storing package counts, weights, PO linkage, and traceability metadata';

TRUNCATE TABLE `receiving_history`;
INSERT INTO `receiving_history` (`LoadID`, `PartID`, `PartType`, `PONumber`, `POLineNumber`, `LoadNumber`, `WeightQuantity`, `HeatLotNumber`, `PackagesPerLoad`, `PackageTypeName`, `WeightPerPackage`, `IsNonPOItem`, `ReceivedDate`, `CreatedAt`) VALUES
('00396f8a-aa0b-4124-83b1-3f391e47a992', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('00d47565-8acb-4cba-81a1-96fdd70999e4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('01930833-5811-4cf5-b6bc-f647fd8a45a1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('01ae68b2-c776-4d9c-90e2-a083b707819b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('01c1e97a-30a1-4188-9392-f6e2ff67fa63', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('020353f7-7dbb-4d57-9286-9154195cf661', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('0227aec0-d26a-4e26-99f9-0f6858874ea9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('0331b0b0-d0fd-4735-a069-11cb03fbb0be', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('03809216-3090-4787-9cc8-7c9b8930a9dd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('0403cf1c-9ddd-4a17-9b20-d524bb491b2a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('0415e233-78a4-4eff-9f74-0d1c468135fc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('04acda06-57b5-45fd-aad4-ac3a8ebe91c4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('05703de9-36fe-45fb-aecc-f1f7d9954c73', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('05cec42a-0880-406d-98f8-c086be86f6f2', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('0615da56-2940-41d5-9550-3be29b569d41', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('068a3588-5717-41a9-8ddb-673f78a37352', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('06e1c7c2-a748-4507-aba3-af17a348c6c3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('07034327-0423-4d00-91de-6cea0ecdd6b3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('071393b2-1be8-4f86-8421-41c17d8edbd8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('073048d9-4ff9-4d3e-8d57-9122efe58b6a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('07880d92-c401-4f97-8875-8c76c60d7166', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('078dfda8-117f-410f-997c-c6bfeab50998', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('08973165-9273-42e5-a4e0-2560f6463231', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('094fd9c2-c034-4b7f-ac49-3e8d6ebdad1b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('0984eec1-61e3-4e74-bf20-77c649cc3c7f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('09e6d139-7457-45a7-981f-9da28532ee25', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('0aef1475-429b-4e5c-8a53-c5dae9c01b21', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('0b4ec2f7-7788-48f7-9115-da38e6a5a4b7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('0b7d449d-cf36-498f-9a88-7776da25ddb3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:47', '2026-01-12 14:45:46'),
('0bc309b2-82d2-49e0-a974-4fe610dc11c5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('0cb8c627-f897-454b-b5e6-8fedc0498907', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('0d20d797-3f78-471a-9452-e44b41a8ba94', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('0dd32325-042b-425d-8e1d-0469f69e32d9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('0de67083-30a6-40fa-8546-4afd3805f3f7', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('0e09c5be-fa37-47f5-ad8a-350c9c4a061f', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('0e2b1b86-223d-4e05-9f13-763c4f280e04', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('0ef76428-4897-45dc-ba97-ca8d5cd0535d', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('0f26fd6e-52ea-49bf-8d75-669668c00209', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('0f6001de-440a-4bbc-a140-ef4a47008fe6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('0f7c223d-d338-43ac-9e4f-0512ec5b2681', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('0fc09b52-5836-43f2-a5fb-9284b3c4a18e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('1057fa01-021c-46cf-abd0-af133e282e73', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('1064ae77-4fe4-41be-b9e3-64140343c8f6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('10ccee4f-ab7c-42cd-9e93-1dd6d4922253', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('114009fb-839f-44e9-9740-fdf3e9fa3d05', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('11aed0d3-6b0c-4c89-8f4f-7a34310d14d2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('11e3f7d1-2650-453e-8a25-f69d6f66a530', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('12170d31-2847-47b5-b8d0-67d1fb4fff8c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('128bd0fb-8839-4441-8c26-2d606347161e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('13011462-15b4-4b59-9212-2e6bf36d79ef', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('130ca999-9a19-48e9-8c4b-932fd49d5540', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('14924cf7-13b8-4c6d-9e00-666c74476f70', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('151fff7b-37c4-4977-af09-561d857d658f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('153c4e3c-3d19-4a75-81d5-1b925993cf0f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('15720985-5336-4905-8e60-36175ea8c3a6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('157ecc2e-cf4c-46c5-9776-8b7d61a376c6', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('159c2eee-adc6-4329-8ea4-a804bbb9ca89', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('15c78801-960d-42bc-9ca2-a2d23ad6abea', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('15dad682-5161-47cd-91d8-940ff784f9c4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('1617b428-9c1e-4f87-9ec5-8ad686956d44', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('168a3726-d4dc-43ad-bb40-d1772df43440', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('170eab20-b78f-48e9-9ce6-386c7e7de671', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('1752224f-5d89-4dc7-8a15-282dc97d5b8a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('17be07dc-f4b7-4dc1-8ab5-992f092f5cd4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('17f95871-ecf5-4f95-adb7-3813c36be766', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('181639b8-38ed-4b99-9d07-61f2829f74d2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('18578bda-58e0-4553-94f9-b45e4009a68e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('18bb74ef-a0fc-4088-ace7-9bee6f5557bc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('1903b9bc-c772-4684-8afb-2617eca24f60', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('1950e2ac-41cd-4b09-a5ae-2c345920d9a1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('1a413814-4e6d-401a-96dc-e19212b8a414', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('1aef6541-5c8a-441c-b9d0-ef3a636f468e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('1af2344f-d604-461c-b391-30aaa9b18eb3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('1b0a42f6-954f-495e-af6e-d0674ce99cae', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('1b67dada-17b2-4a1c-a637-9d0f949f938c', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('1b846cad-529f-45b8-85b9-905dcd8461d2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('1b9865f2-901f-4eb8-ab67-7012f1ddfec4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('1be0a2e3-d70d-444a-b4a5-b0938cdf2473', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('1beba038-3d54-404c-b918-a88933379d25', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('1c2c9367-f3d4-4145-a64a-53e630d9fc2b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('1c69cc33-de9d-4df8-8ea8-2d1c0876ab1e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('1c87dd07-e7fb-4184-ba98-5f480816001a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('1c9f4ca6-d033-4dd4-a8ad-0dba06faca83', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('1cb2c348-137f-4461-ab05-614ca5c138f1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('1cf8c4fc-6a81-4dc4-a711-e9dad2ba4dc2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('1cfa982d-6a00-4e83-9e4c-3e83ddd78a7a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('1d43d55d-d6d0-40de-a5f6-eada7894004d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('1e2a4c9a-3eb0-4228-b4f7-9a515215a532', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('1e87441d-2f49-450c-8180-b3146afa5f78', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('1e9a05dd-ff86-40b6-b610-9e53a55563fa', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('201d61c9-86b0-41ef-912b-b66aec9a1268', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('20944f94-b286-4881-9f5b-bb8b978fe19a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('20c3641f-1c21-4c5d-b4df-dc2f74bef9cc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('20d6fb15-6fcf-429f-8ec7-f4d3a40774c5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('212568de-700c-453f-8de4-a02f7a1795bb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('213382de-e89d-4311-8a06-577c0409a07d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('2228bcc5-3b7d-4139-873f-911d4d660621', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:47', '2026-01-12 14:45:46'),
('226f8391-cdbe-4c4b-b883-3c815729d36e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('2350ee75-2038-4678-8b1c-7532390197df', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('2391075e-24c2-427c-989c-eb4a61f3d1ab', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('23d40510-56f2-4cda-9db5-039a96458051', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('24593f25-c30f-45b3-b982-de200aeb5098', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('2567919d-a7d0-44b5-a921-9f70a0d1e5fb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('262476ba-c403-45ca-8698-a11882853d06', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('26381cff-609e-4979-97dd-fef7f32c9d60', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('26512314-c808-467e-8cfd-08e1ae3d6029', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('26598960-e477-49e3-a850-0c3602669fcc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('26c6a1f5-e03f-4fa3-9969-fdac5e2d455e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('2759a55b-189d-43f7-b628-66be264b2d8c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('27b597da-a618-43e9-84fa-23e357f01cbe', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('27fe9e5d-95c9-40dc-81fa-b8ac1cdd027b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('285347e4-b407-4c99-a723-c1192fbeaab9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('28f97fd5-fbed-4061-a4a4-8dbc0e56a031', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('290238f3-82b1-4ecb-bad1-bdac53771b1d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('2952fdab-690c-4aec-af2d-d99f6ff98e72', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('298ec7bb-7bfe-40c8-b0aa-459a5332e87c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('29e7cc58-72fc-415d-a110-3b29ae6e8a36', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('2a06869a-4e21-4abe-9da7-b06f87f8819d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('2aedcabd-1c4d-4378-be4c-b0cf745da776', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('2be1d15b-baab-4603-b3a2-728e589b8080', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('2bfde527-8ae3-4fa9-a35a-586a37361d13', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('2c1f5479-0f42-4e12-a79f-53ad238447ec', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('2c35f0dd-0a04-481c-9680-fd8d7e9afec5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('2c7b44e9-2348-4e08-8c38-d9dac047df80', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('2c819bc0-fcf1-4d16-86b8-c08745a2a707', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('2c84c157-af9f-44bd-9721-0223cdaef51c', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('2d634b81-5ff8-40bf-8cd1-c03463fa6395', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('2d6b2385-25ae-4e90-9c9b-5e186bfcaf8e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('2de2ea9a-c784-40bd-82fd-7a863ba3088c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('2e4a35f4-738d-4c19-99c0-af00eb1b507d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('2e53e174-9fae-462f-bf9d-ac963fdfd731', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('2e9a1b0f-4315-434f-b470-e5e06a170a1c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('2e9a7001-0417-48e6-a322-b6e34190568b', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('2ed200a5-a147-4d87-af16-1ca5a2e39a2b', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('2f428636-afd1-4d90-ac6c-69bfdf86a5bd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('3048a215-d45e-4ca6-b89d-03b9d26830aa', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('305a2f22-4d36-4dda-a22c-bf554084230f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('305d4a2f-da53-4889-af15-2fbf121c4f5e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('30b695b7-e984-40ab-a5a2-e8ad9744e7a6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('31af33ef-db76-42d5-8e07-83df93d741e9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('32386e6d-645a-42f2-b7c5-706fbed296a9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('323cbe94-4181-4117-8f3a-3576fe9a5906', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3248d653-3e94-43e7-9925-cf17327dc6a4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('3259a112-1ea4-412e-9048-90c7c52c4fdb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('32a79c3a-6200-45a0-b299-a239085f5048', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('32b6da94-6fff-4fdf-92c1-801df45ccf80', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('336d4edc-d067-45c8-b7f7-8bb9a2c0d6a2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('33d08815-7e7a-4386-946c-4e6141da9827', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('3414ce58-19a6-465f-84b0-61d9cb3be13d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('3461d1ff-a2d2-410a-8534-4b30037cc4cd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('3463ec34-b9c3-4212-a087-44dd8b7adff8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('3472bb0b-1a60-45cf-a5f3-b3f84f141a36', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('34e52301-6efd-4ee6-b2a9-c6e308f08f35', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('34e7d495-44ab-4c95-8326-2c43106b475e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('34f24949-caec-49ad-a318-ef212b8c9632', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('3527e945-c22b-4338-8bca-7afeb5e17f42', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('3536031d-cd52-4436-b3e3-40eb27fb4cf7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('35765d42-77b4-4e07-8f3e-5a276029c0cb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3580f55f-6989-40cc-8199-61d11960ddc0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('35da1fd6-e132-4a4f-8d3e-26dab8077796', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('35df914d-9ad0-47d5-8b52-1219dbeecfac', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('36094af3-2760-44b6-b234-17e59f2d8640', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('363fea1b-3ac4-44de-b543-7c8674fe3c31', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('364e201a-03d7-47ce-848e-bf313f6ee653', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('3650d2ec-fb5d-4491-8fa6-92e12be97713', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('370387e5-1697-43f3-a427-a81b1b909f5f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3760a630-bd29-461a-9ed3-a94b856ea2b4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('376ab39b-6ab0-4f99-8a18-f32d05422877', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('376fb68d-5192-43c7-8dfd-0353ed7b98ed', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('38093b16-56dc-46a6-884f-101321302bea', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('382e27c5-634a-4475-9afb-68908fd8218f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:01'),
('384a544a-c98b-4bbc-8276-ba43a6f17a34', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('384c35c1-a918-4a45-b2a9-30a7e9bd8537', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('385a2f81-adb1-4f90-8454-e59e6570a922', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('3882b9e0-f4f6-48f3-8d53-7992e568819b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('388f018f-31c4-4847-b305-380d6123f323', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('38b9d907-437f-4801-9201-45160de299f4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('39860546-a40f-49d4-b174-0289c0eca045', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('39a8c693-c6d2-44d9-95e9-69e030c63392', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('39f759d5-7265-4def-9f57-b7f555bb5d4f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('3a5a57d5-5b4c-43fd-bc3e-f94d0f39d4ab', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('3b3d5b2f-c6bc-44d8-ad62-a1bf868fd728', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('3b3edbb9-7861-49d5-855b-e37adab3ccd0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('3b6b5a6f-c8d4-416f-bbc9-991ea4320831', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('3b7244ff-2c69-4a19-a171-c41f9876823c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('3bdf9956-33da-4fe9-8a72-3e433de0880b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('3bfe2b7a-b72e-4fc0-b567-4a61a6923a53', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('3c680717-01a3-4015-b7f0-2099efeadf7c', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3d66fb10-6dd7-4a35-9fd8-de606d9f5669', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('3d7b4fcf-af66-4af5-908b-45b7350be3b9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3dcfe5f2-5cf3-44ba-b2ea-f8471662fe89', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('3ea96aaf-169c-4421-9794-93f38853302e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('3efde49c-1c07-4157-93d0-3f75d16ba9a5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3f35695e-6c01-460a-a5c9-94273e4faaf1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('3f6fbc70-d2d9-492c-9fb6-b34355d81526', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('3fa87925-20fd-4e2f-b82b-12e4d08a2eeb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('3faf9681-e4e0-43fe-a4d9-64cb4ef3b1dc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('3fb15d0f-6009-4c37-98bc-94edd44b085a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('40a07a72-c7a9-4e3f-8bfa-9fa9efc3c948', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('42830cf7-c3e5-49f5-a164-34f063cd4707', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('4294ccae-7b05-4428-92fb-410844f975bc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('42c20931-ac2e-418e-9e28-19fa013d1f61', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('42da7da3-3cb7-4a4c-bacf-98349d372ed5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('42def54e-b92e-4441-89e5-889e838bb05a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('4339b2ed-3237-4a2a-b7e2-f64d6093dfbe', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('43b1f220-1fdc-45e5-a206-07d24f6b5e0e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('440b9a56-3376-40db-a903-f36265014c15', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('44a283fb-1b3a-4571-9bf0-61bede608094', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('44f30748-623f-41d5-9f25-c9a10e5728b3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('454218ca-1c00-417a-ae8a-b064026c97be', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('462ebb91-211a-404f-b329-d18f0ea35b91', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('46562526-10e8-44aa-adb9-8621fb11c275', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('4668867f-2079-49cf-b8c7-92ab0f59b733', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('46922252-56d6-4a03-af98-2d7966c757da', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('46c06938-8b5e-4ed9-9b84-b363ba7252b3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('46eb62d1-b0ce-44b3-bc53-a70432c67128', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('471926e6-7ef8-4686-b2d0-8adbc945f15e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('489e1378-fd9b-495a-955a-393ee5affd6b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('48bdceae-8be1-4fdc-bf8d-bc14df42e696', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('494ce8af-05bf-4357-8730-20bf7b437d8d', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('49be514c-39a8-4b2a-964c-758c756d3d6d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('49c880ef-2e55-4954-9423-28d1179b698f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('49c8a86d-13bd-4efd-b1c9-60e220ff3668', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('4a46b141-f194-49a9-8ded-20ce59f1f5d9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('4a5a2caa-8849-4ea9-b0f0-5dbedbab6af7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('4ae1f991-bf39-4903-8b0f-6c3550c79749', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:01'),
('4b0251ae-ccd9-4596-a4df-1e95c16c2753', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('4c73b510-a5a5-4342-84ea-15d0003a99da', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('4ccd5a5a-2d69-4df0-b93d-68752615b1b0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('4ce1a5d5-5464-4138-aca8-e1c9cff2b1b8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('4d28a640-c76e-4e7d-8143-97353acd4c7d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('4ec7f007-6282-44bb-a675-223fa056a010', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('4edb5aaa-a38b-43ef-96fc-38fe5cbdd8a3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('4f6a7c49-67f7-4188-994a-81311378347d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('4fa96315-a905-481f-93e4-5f6def7a7696', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('50023bfb-0f82-4638-829a-a1b52335eb6d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('5012e8a5-75ca-42cd-9132-2b19415cf32a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('50a128a8-3cfd-473e-bde8-15b931aabcb4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('50d9dd17-838d-4410-82ac-a14bbd93df29', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('51493997-d819-4690-a695-e1a9822db160', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('519d78a8-9687-4d5a-985e-18f07e7b45b5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('51d6b508-047e-455a-8879-881965a5a56a', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('5246dde9-6179-4c5c-a419-d36fca941457', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('52971a38-6b85-4c02-9f97-4decb182fddb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('52d4eb8d-e3cb-4aa4-a5d8-bd0197726027', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('52ed060d-a32c-46da-b0c6-f249b945b750', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('53d649a7-aefe-4e91-bf3d-f335f587ea0e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('5405455c-8ff0-4236-93ec-916c1f5200b7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('541c7948-9ca9-42c6-8437-b4d407351dba', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('54518b38-2e43-42e9-ab31-06a5463abd42', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('54fe2130-887f-4122-80e4-249af1855be3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('550123cc-e407-4cdd-963e-a267778dc512', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('55250062-42d7-42a5-b4e3-1ab610516680', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('558c3b6e-dced-4728-bb2b-50d778437b6e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('559f9d37-8729-4ab5-8fd5-8e192566aff0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('55b80d24-fc24-4bcf-8772-f4bdb36776cd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('55e7c8cf-6f92-4e96-a2e5-4f2766ab6c93', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('564499a7-8e8c-4b8e-a2ee-bab365b43ef9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('568a4af7-d698-48ca-9525-510a8ee541f6', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('56a4ef1f-b349-4557-a452-e63691631b3d', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('56d1216f-b759-4836-a6b2-c64a4420e39f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('574e0f7c-57dd-4981-9435-94cb97d2eb28', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('57552267-5ae6-42b0-973a-dae757754c30', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('57876b4d-77a0-41e3-afc8-3a21f5b7e09a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('57a6e52d-3837-49df-8a3e-0ac63803a2c7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('58935c48-5023-4413-b730-d79226cccb79', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('58a8dd58-43a3-4c11-93a4-ac1b9ba47ef9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('58c56882-b7b3-48ee-a426-a5f6a9a2e019', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('5924fb80-295e-429a-970c-9827299ab202', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('59463aec-4c47-4fb0-aeff-8f330ad637cf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('594c9074-05fa-4a88-a78d-5c397237c914', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('598eb8a8-5ccc-46ad-b290-6f69b1ed655f', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('59e2ef7c-af93-48e8-9cf2-e39a6bb2d179', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('5a1a7615-7a18-434e-9c5e-2f0628a222fd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('5ae4e4cd-39e9-4803-8e7c-baee2a5bacf2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('5af00a57-5aca-44bf-a22a-8e20416e2516', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('5b1faf4b-3f0c-4ffd-904c-b80feb725207', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('5bcc6afa-02a2-4aca-8241-b2c2af991b7a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('5c1aee7a-764d-4f9a-92c9-0fc677865c9a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('5ce95e37-d060-4e11-b847-e9c188f9ada1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('5cf9e57c-e458-41dc-9e25-bc5935abcdd1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('5d5e0311-f965-4198-a193-660eca8e4578', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('5d837796-7b27-41cd-b894-87e0a0f8482c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('5dd11296-471e-4b35-b040-94839810483e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('5dfc54ed-e126-4acc-9719-53508568d3dd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('5e222b7e-ef0f-4318-9a53-516e7388bcef', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('5e6b8f47-98a1-43ec-9c8c-111b7e3059b2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('5e761962-e027-4b12-839d-2f26c00fbd23', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('5ead87ca-a955-47ef-84fe-8d8d6cdd63b5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('5ee00b67-7d62-4783-8a9f-810049ca6e77', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('5eefd535-6773-44e2-ac36-fa0288589fc6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('5fd1c602-2722-47c0-9fe2-2b18437a450c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('602348ec-dd00-4cfd-82ae-3de2984a4f6d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('60343fb9-d41c-4671-b170-9744315cb5e3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('605757a7-54b8-4773-b507-c884cecc62f2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('60686138-9587-4702-9875-26079bd10d5b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('60c1e397-d471-47c1-94e8-90d88637213c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('60f8019e-e04f-4fb4-bb06-069b2d3165e7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('619740cd-9d7a-41ee-b180-414d35653a8b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('61eb81d8-a03a-4f96-b927-ab43ee383cad', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('62f99d41-71d2-4827-84f6-bbf968a3de33', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('6303c04d-3dab-4576-83a7-0fdc2d05b5e8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('635af029-c32b-4f03-aaea-aa424f444e38', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('64683a23-6f4c-4da1-ba8c-04be1e4badf9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('64f6bbae-16b9-46eb-9bb6-7ebceff413ee', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('652437b4-f41c-4800-9341-053d1df717dc', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('65858dda-8919-4748-88e3-bc29d764c453', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('65a93a49-bb0e-4441-9f2a-fadab78abb2d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('65ba1c82-185f-4069-9cec-af5cdc99f3d3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('65c46063-9473-4cc3-bcf5-2890152c92ee', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('660feded-8b1a-4e83-a96a-76a036f4fb20', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6681685a-1a53-4bab-86ce-ce94a965bf40', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('6684498a-7299-4b10-8f8c-7ebe5d71537f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('669b3571-1b11-4275-b536-0826619fb43d', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('66e13419-82c9-42de-9cce-d3126de194fa', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('66f41c90-05c1-4dfe-be9b-6c59a418e487', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('674aba73-1965-484d-9eff-40cd12031de4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('67629264-bf9c-49ea-8e9f-1d60a0bd3290', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('678718c6-02b6-4259-9052-23c40d8574ea', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('678cb86f-981d-48a3-a827-b516cec05f10', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('6845151f-d891-45c8-b6f9-218558eec635', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('68c47701-d9f8-410b-839b-c1075df11f39', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('6931e61d-96ff-4c10-8e5e-d897a9b45330', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('6a2cbe1f-62d9-4da6-90b1-6073792ed2fa', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('6abc85ab-e0bc-4002-9c3a-82cfdfdb53ce', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('6ac0d26e-664c-4ee3-ad65-c115196282bf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('6ad7f588-1151-4f36-ae9e-40a43d611a2f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6af6c6c3-b59f-4ca0-a592-e596cb286604', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('6b1c182f-e229-45e3-83d7-1c7d8a01a0a6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('6b2cf9c5-4e9b-4f0e-8b19-e8c84b7ce952', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('6b55276d-372b-4135-842c-a7b78a92858b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('6ca87d03-9a04-4695-921a-6bf1d9db7b31', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('6cc7ec6b-184c-4c92-b564-68e3a98171f4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('6cec82c2-9f4e-401e-a4ac-44814106cc9b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6d5ced6a-8790-4cec-b3b2-91c9130a56d7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6d5f7f3e-4b9c-493b-a24d-f7dd0674834f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('6d629340-08eb-4e01-8885-e61aba6496a0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('6d78c271-e4f9-4544-a9ba-a3c8bc61dc6c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6d95342b-e597-4362-a10c-ce78b85e368e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6dc23569-b5fd-43e0-89f2-2654b23e7fdd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('6dde66f2-7d68-4144-8e61-2d751d8c173c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6de88732-8d7c-4bd4-9ee4-11ac0828fdb9', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('6e065778-2690-45a4-972c-3d531132d536', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('6e1b4066-1ead-41f9-88d3-8467d5c62749', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('6e47308b-ded0-4da0-bf74-fa2d4fa825dd', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('6e537326-1f55-425d-a282-cafeca72e559', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('6e72c02a-1b2a-4ce0-a83b-b9a7b2e03d7a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('6e9112cb-1b2d-4745-aff8-0a701f36dbba', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('6ebf36c1-8b31-47f4-87bc-b2f8fedd26c6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('6ed62b86-216e-40b6-9a7a-7d1375653ff8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('6eda4fbe-311f-4505-8669-162e3a592bbc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('6f867996-c2b1-4390-aa76-66b96fb6a2b7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('6f94eab3-514a-43f2-a4ae-d1b9bb40dbb7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6fb293b8-0151-4d47-993e-bf9459abd6b2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6ff21827-8fea-48f7-ac2d-5dba38fae0cc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('6ff46411-f134-41a0-921c-314d7635bb99', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('70495288-fe09-4381-bd81-06602bf43692', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('707f5d33-4ae5-4e47-becb-a8695251c46f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('70b2f0bf-586b-4b57-86fb-6af653fcf23a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('70e28893-e2fa-41ea-ad38-ec36f017d3f0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('7121d430-65ad-4998-8514-684c32a24554', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('713513c2-5b89-4712-998e-eaacb8dc245e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('714be136-7529-401c-a347-bc9dfc63e4f0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('7160214d-565b-4d03-b20b-cd556ce07547', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('71ade942-4632-41c2-947c-b33ea96506f4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('71b719b4-6038-428b-9dbb-ddfca14c00ab', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('720be457-6a98-4b01-9242-bad700bac89c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('7341234a-b105-4284-9213-d66bcf0a4874', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('734d123d-612e-4efa-a917-26b9887ea52f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('737ae626-8823-4876-8cd8-f0a9c4d7c642', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('739188a0-5724-490d-8479-487f69720668', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('73e02c9d-c884-4f95-af1a-f11d045d33dc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('7463e502-2c74-4d7d-b5aa-da0a1940e5db', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('75d404e8-fb48-43c3-b7d9-d7adf7f266c0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('75fc5c5c-4936-4de8-9797-90eb8c5951fd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('76237fb1-9e95-4d2a-a91a-033ceb4a5b0b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('7682e6dd-d691-43ee-819f-1d96961ab84a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('76f9ca44-9296-4650-bc4f-1ba4f5089bc2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('77711f2b-8946-4060-af52-9a8c6845d320', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('77946c37-5638-421e-9df7-fdafd1ee1016', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('77b7c31a-b6f6-4838-bbe7-6972faf04a90', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('7808224a-4d17-4459-be87-440f7c72cbd5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('793fc4ed-5a0d-4f9c-92a2-346f3072a554', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('7a120791-f23b-4be3-815c-9f9411ae7397', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('7a444cd6-408f-4ce9-958c-31e64f00ca24', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('7a639e6c-e287-42bd-90f9-e41f2f20fc36', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('7a6c0bc2-966a-4424-b5ad-012770a281bf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('7a7983c2-4a1d-4204-a3fe-9fab0871efdf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('7bb0d691-d4a1-457c-8939-75e8ac4b69c4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('7bd70412-b9be-4bb0-bd57-8de2888d77d8', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('7c752a3e-80a8-4489-9744-2b39850953bc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('7cb22c1f-3073-45e4-aaba-5ffb68d04ad3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('7ccb9ee3-ace1-4c61-964e-3f433a6d359e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('7cdccd13-8ebb-45ec-8153-9c8b9b326993', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('7ce8f791-7c8e-48e5-9144-61329435d8a8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('7d1c3762-8887-4570-97d2-b3f44c2c737d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('7e4273ae-a772-4e7c-93ba-6daec32c035c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('7eb25921-703e-4112-abd0-041994cc0f39', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('7ec641ab-36dc-4437-bc41-e79dcce362d6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('7f1c91ca-3fd3-4457-8f2c-91ea7242b4a2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('7f620947-5e63-4e1b-893e-09606fc2f337', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('7fb7019b-0b22-4e2f-a8ba-ff9bfd722db6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('8078fe22-5fbd-4855-b553-07f0640973eb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('80a3e0d5-5dfa-484c-a03c-a366a615a626', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('80b57db8-e6e4-4995-ac5a-31d9c1a870a9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('80c4e2dc-91a3-4a44-a87a-a448baaa76d8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('80dd7039-e2d0-4d5a-b2f1-1620dfb57f43', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('81bb0498-fafe-442d-b0f2-aea79e38af93', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('81cf3ed8-9b01-4e59-b30b-75f6d170e8a1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('820cc07f-298a-4f2d-b0b1-bb0278ce94cf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('82fd38d5-10eb-4c21-82cc-89d98985e691', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('8365a83e-06a7-45a2-b207-bdd2e64bfdde', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('84016eb0-d870-4773-b332-97b76d422626', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('841a4911-ee26-4e3f-be9a-7371bd2fe996', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('84835615-b151-411f-8e2a-a20a639393d0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('84883889-6459-4c8b-9005-b3d18573a90d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('848b76b3-8a0c-466e-b371-39c212bd19b7', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('8499bcb0-2b59-4fa9-86bd-e06b08e57150', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('84a1257e-6481-4690-a996-bab4d6eb86a3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('84a8f459-9aa5-455c-8faa-a4c5cd8a6b24', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('84c03c51-1228-4b16-9ca0-19676d118407', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('84fe98ca-27bf-41ce-84ca-0a4415651f78', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('850a48cf-21ab-4c73-96b7-8e0ebfdcddf2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('852a30b5-bb85-405c-b5a5-b2544b8e4a9c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('8580aa39-2e3a-4409-89a2-b38f73de92b0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('8589fd12-144d-449e-9ff8-4961d973218d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('86029ccb-113e-46d9-afaf-eeb1ce87e57f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('8688c85c-e8e4-4898-87c1-d611a557fee0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('86cdbc3e-55b7-4b8b-8592-34e1ed36f37d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('86f6d685-3dc4-4395-a10e-bc0b68f947e2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('876db954-201c-4970-8260-874f7841e0d5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('87ab6eb2-c86a-42ba-bdb1-bade296723d2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('87eb2223-86da-4527-a0e0-bfa33733fb40', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('88a95496-9b4a-47e0-80bc-04d101f48ebb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('88daf3de-a6cb-4f2a-8e8f-a2af3689a80c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('8932b871-a584-441d-8137-900363c9f89f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('896821ff-cea0-4c3c-be6f-dc1d0849fe4f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('897c68d1-3387-4b47-ac01-f7427428e543', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('8a19106a-49fd-44e7-870c-955ca6b6e752', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('8aad2e81-3b86-4f06-8e3c-4d375a6f640c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('8ab43923-2d1b-4811-8ee1-cbc826ff8925', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('8ac209c4-86e5-471b-b787-dcf509bd8ab8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('8ad32bc0-1fa0-4ef1-9388-fc7de6cfb3d5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('8adb0093-2b88-438a-8126-5ba5c88d8c9c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('8b1557f0-ad5b-40bd-932c-034eb500b470', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('8c2e6d3e-7d25-4325-94ae-92b82aa8c70b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('8c4adbec-5035-45b4-8a59-fdba77e9bf5d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('8cd77248-b1d9-4f18-9c1c-ad7e8b3dae80', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('8d02e17d-7c07-4d56-83bf-9b76661a9ed1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('8d5c6a9e-d56d-44f8-afa3-2aa6ad339ddc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('8dcbd40b-69ac-4688-ac0c-f60ac3ec890c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('8e0984d5-e4a0-49eb-8c82-ab3583a2d1c2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('8e15bc33-da65-4eb2-8e2f-ee310b15c709', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('8e37e75f-a640-4a2a-91cc-5cd593846b54', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('8e5bf06e-b4ba-485d-8ebb-fb5b63c1bc6b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('8f029de9-4a93-41a1-b40c-e6326df25e60', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('919a8df9-8495-421e-bf56-4d6ad0c09dd6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('91cf154c-a0de-42cb-af29-8b18a87ef103', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('91ffc1ca-a664-459d-aef1-84883207f2b8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('921fb605-e414-4e17-82b0-0e1d86265e60', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('9236bfa8-0b28-4834-82ca-befeed1191a5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('927480e9-88b9-4c4c-9490-e9e3167385f7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('9283f8a7-1c78-4dfa-915e-a493f9d2b3cd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('92a7a350-8af1-4358-8a8d-3306cff21e72', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('93036eb2-a9c4-46c3-a97d-bbfa3f03ae4c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('933419cd-ddba-4897-9267-03741e9f1321', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('93431b36-14b1-47e7-a1fa-a3fb8dd1abf5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('9396ef5e-2aaa-4a71-923c-886748a734ae', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('939f5248-2ea1-4244-8e68-258c9d5040cf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('93d00400-e04a-4697-b87d-77f554a3d0ba', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('943ab2f6-721f-425c-8d92-058021fef210', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('946abfd1-df39-4bac-a248-44f869526d3d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('94dad2d8-aabf-4786-a089-262d738fbab4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('955c81c7-8616-4730-a351-36fd415dc686', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('95915992-aa67-49f8-a63c-2a2e1ecb5da3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('960540ce-8d05-4256-922d-c6d986e6e757', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('960a7e6a-43cb-4f99-99e3-87ab31e15623', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('9644c393-27dd-4ebd-a625-bbfe4466a6d8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('96a281e6-6302-41c2-af19-c089ede9326b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('96ffd5a8-1e8a-4e77-94af-3c84bda4fa29', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('97170ea9-39c2-4e77-bd7e-3f83f341628c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('97408f8f-0b9b-46f8-abe6-1946c1a441b5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('977ececf-2006-4439-bac9-2654ce59e389', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('979071cf-b55c-4085-a771-61a0416413ce', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('97fbe47f-2c5e-4853-af47-9801f671525a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('987910c3-ebfd-4e8f-a7fe-c9188a900fa7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('98d8283c-1f7e-4fc1-bed4-6f41fae4b451', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('98ed0479-c011-4041-a03e-5933a77e1725', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('99054593-cfb7-4885-a0d7-698861e5899c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('993d49b5-8c5e-4210-872a-7368ccd04851', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('99503555-d45f-4df6-ad0c-e817529192a8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('997dec07-66b5-4d6e-b359-f749c1ee2763', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('999e8b47-f317-4663-973b-b761ce6cf4c6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('99a3d64f-466a-4a0b-9dda-3c39e6d1b252', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('99b2a560-86cc-418a-a078-a476cba27c7c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('9a24d86c-70fd-42f4-bb22-4c705de86889', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('9a29f834-6480-419b-8f31-04112b0c594c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('9a7ec538-9285-4e21-8d49-dbc433fa7025', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('9ab8f041-4eb2-4c1b-b7c9-68ca161398a9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('9b44ec87-0739-49d0-9046-34c12883bfe4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('9b4a427e-e9ed-43dd-be34-767cd98e6b40', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('9b8211be-5b2d-4043-a8bd-a9890c80f1d0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('9be4fe5e-a85f-4525-ab2d-8e2ea9ec1713', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('9c8fb35f-3e61-4fee-939b-c87f5faa0192', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('9cb78a1c-c0df-43e6-9017-c02b4c2a9347', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('9d31a5f4-38d7-4660-a1b6-e90686fe4111', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('9d70aef2-4d00-4590-818d-acafcb166795', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('9dc05410-ebd5-4801-9fe8-3d133f5461b0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('9dc8f0de-ff15-4987-afa6-b08f0f098323', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('9dfeda9d-b5de-404a-b809-556252962d63', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('9e56a852-9617-4e08-9a78-8831bb7cdee5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('9e7ef3fb-03ce-4584-bea7-b81cc30831ab', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('9e974425-fed9-4693-91c0-a2ebe0f535a8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('9ed7172e-def8-4f06-add9-23a91683f7b9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('9f0d186b-42a4-4c9a-8ca1-2eec999e6371', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('9f32e03e-10c3-4425-8002-857ddfa642af', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('9f3de3c9-be7f-41ec-88b5-2d3da909d1ff', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('9f565ee8-6c74-4b62-8d5e-f7b0c21e40d3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('9f7f5434-4314-49b3-a3bd-b18e59a1e582', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('9fe6e6d8-838c-4af9-b437-778e82750a67', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('a037c09f-fa1f-4634-9e55-efa19fabce5a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a078d780-6343-4ea9-b349-887ae2f7b842', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a1552205-5aa7-4443-8e42-cab50a036327', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a1b7f0d2-5e5f-4788-9e69-f3403e31f8c7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('a1f3b429-622c-408f-b4e0-138eafb658fb', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('a2488438-a72c-4412-8586-a54480c4ae91', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('a28b1f3c-40b1-45a1-b6c5-c6ac3f31d6a5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('a2c81f8f-9b00-469d-b82b-9de8cf166db8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('a358133b-6c65-4111-933c-b1b7803df1e5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('a3acc0ae-061f-4e23-ad81-a74698b68f4d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('a3cf17f0-0181-448a-acf7-92f76b338406', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('a3d47a9d-c40e-4764-82f8-b0242aa67047', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('a3fef1b6-7521-4c89-ae68-c4fb5f9a2361', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('a423c163-3259-422d-9b82-a76e2541c277', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('a42b51af-d49d-465e-9625-6d2848e02a46', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('a538d6b2-6a32-4804-ade6-e5064aaf3db4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('a5a702f6-968f-4f21-b18d-a08a775b833f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a5c50e56-1488-43a3-8b9e-b83c87467446', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('a60510f2-73a1-4d91-bf87-f0d70ad96d1a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a6d4c6f1-da6c-4d32-9dbe-141f3c22706c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('a6db2467-715e-486a-95f7-7f0b44c390c6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('a7e175b5-5c38-433d-87f6-fb9e52f5a3b1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a7f3ab27-00f1-4828-8bdc-8201757ced60', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('a8cc94b5-78b6-4a2a-bbdd-e816862dc0ef', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('a8d32f55-b563-437e-880d-88d409600218', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('a9885dcd-99ff-48e2-913d-fb867b554f04', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('a98ad395-e059-4ec7-a2f2-559b61e5942b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 12:54:42', '2026-01-13 18:54:42'),
('aa8228f3-6017-4d79-b053-e95eb386d6fd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('ab1bb943-145d-45fe-be77-3a8e227a446a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('ab94d1c8-143e-4dfd-be17-657444606eca', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('ac35edbe-67d5-4c5e-9b1e-96b77f58987a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('acc1b8f2-1e6b-45f2-8c06-0902892f0fe7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('accb7504-bc01-4dbe-8cda-991f47a1fb38', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('ad173da2-a119-43af-8031-6d4d7a10c832', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('ad763bd2-bbe8-4a8a-b7f6-2429ffaf847a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('ada5d7b1-fc15-450c-a0c1-5d21104a7778', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('adbcaccc-3323-48a7-83af-e40ab0ea8976', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('ae380f68-df08-41b7-a7ac-7c6e1908b6eb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('ae824c72-43f0-46cc-9725-78c0e7b5a382', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('ae8c05cf-da2d-4ffc-8e86-c1b6a7745da4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('aeae3f4c-268e-449e-9a14-e3952949adc0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('aeb60f1e-8b0f-48f1-a182-e55677e24d8d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('aeb6f349-8c29-4808-b8cc-a9a57836c85d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:01'),
('aeb94c86-8e4b-4082-a89c-fa277e65bdda', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:47', '2026-01-12 14:45:46'),
('af51ebb6-639a-4321-808f-98e0ab6255a7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('af7bae18-58b6-416c-abc1-1e295d9d8cbc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('af99588f-478c-4197-91ab-a53e92cf57d5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('afdabc0e-6a88-4847-8bb7-1c7badf21a4d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('afdede74-71e1-41a0-9b0e-c2292ae5dbda', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('b01c2277-92eb-4c78-88b4-5f145d01b2d4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('b044f2f7-1e51-4873-af39-3c5a9155b901', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('b0e0d7b7-8874-44fa-96de-adab30330d68', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('b139c26e-de2a-4223-9290-faf734edd72f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('b166b581-e37e-4b06-83a9-b2f79573b721', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('b28663d6-04e1-4221-a704-17c82e51be89', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('b28b4b69-b764-450e-92c2-b51abaee0046', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('b32430e0-0113-4721-973d-9fe9bef4abf2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('b4682330-f910-441e-9b37-b8b149a9bcdc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('b477bca6-8960-402d-a27f-f8ecb494a4ed', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('b4af4ee8-5f43-42ea-8c31-306750985b18', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('b4bbeb9c-a79b-4b69-a3e7-803c73d10b98', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('b5d89110-9812-4b92-bb2f-9eb25c55a910', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('b5d8f815-54a3-4748-b4ad-af93dd8034ab', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('b5deb226-673b-4165-8f34-02a3ef351d76', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('b62f80cd-c75a-429c-8f56-d8d582ac9ca8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('b673f7ee-e977-442b-abab-86eacba18dfe', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('b692e894-d3ca-457d-b709-0775046c684f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:47', '2026-01-12 14:45:46'),
('b6d8a744-de6c-47b3-9784-e083bff853df', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('b70c242b-af71-46f2-8ce9-4a96fa782b9b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('b764dcd8-8a02-4dec-92ec-885fc63046b1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('b7d737ee-0d73-4b26-b39c-6be699634037', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('b81a5b5e-6f09-4d54-88df-74de0eb7a0c8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('b81fda26-08a7-4471-a86f-c401fdb28a50', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('b86d782f-e073-44f9-a622-c64a117bdf73', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('b957bde7-ea46-49ba-9b24-c318ea143a2d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('b958ccf4-9bf3-44f6-95b9-5407274fec96', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('b9755363-a246-4762-9f76-1fff82cc755c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('b9a328d9-240f-4515-9981-31cb47925e7b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('b9defabd-83bf-4880-b26e-42e2a9060dbd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('ba8081a7-8cd8-461e-af38-de06345b3bee', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('bb31265b-7713-42ae-ab49-98f65ec6f064', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('bb47dbd8-00c8-49bb-b836-5b12992f3a62', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('bba39b3b-9b22-4623-95aa-0827d0d88147', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('bbe1a3d6-fdb3-4b22-9c13-be5af1491ac0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('bdc66d80-8984-4b00-812b-2af09e5a6671', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('be2c87d2-09da-4844-9f2d-e35f5a9c002d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('be4243d8-61f0-4b30-a14c-865f2bf77cb9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('be4e7bd6-5a18-447a-9af2-71c37b67cdce', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('beb47198-0cce-4e04-bd43-7aa8f17fc0bb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('beb56b5d-d237-4950-ac39-b70cba1fbab5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('bec4076c-2a45-4344-8e5c-ec3b37ef5786', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('bee58070-2dd7-4e96-ab5d-a04a3c1a9154', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('bf4df324-bb5e-4d70-9dde-e4da7f180178', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('bfd74098-4ad8-4854-8c41-4d6f710afd9a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('bfd92763-3716-4a66-b381-e3207cb39f30', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('bff6bf0a-672b-4ec4-8629-54250394e9ee', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('c042ece3-7deb-459f-b1a6-8379551c06f4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('c04884cf-c251-408e-9975-424f86447621', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('c082d2b5-34a5-4423-a693-f373c62bc8f1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c0a23558-1d86-47ff-8cf7-126939911510', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('c0e61545-3b7f-4d02-a8d2-350605b21066', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:01'),
('c14485ea-61ed-4c79-8a74-a9554a19eed0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c154feeb-ff64-4a42-8c2c-69324401fdc7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('c22bc86c-7ab8-4c02-8be1-547a900de96f', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('c260d4b3-43ba-42cf-a00c-d91e3fe84105', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('c27ed283-a71f-4724-ac3d-36ecc543edb0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('c2b1e77e-aa45-4ba3-b010-1490c5ef4d2f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('c2d2c309-3585-4dec-bd40-042184610075', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('c349a06e-20ed-4ecc-a176-db0f071a277e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c39eb116-2cf0-4c49-8e7d-126db45a53b0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('c3cd5ca1-08f6-40a5-822b-3dee13193e9e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('c403e63e-de17-42ec-aa44-5ea8bb987c22', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('c46c1bd0-5594-4909-8473-5b48d60821fc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c48353e8-f6a9-45f6-9b29-a9039dd5346d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('c4dbffb9-f3c1-4a16-87e3-2c5741c64983', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('c4dea31b-3612-4667-908f-aa9bf6071e5b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('c50de904-fa5b-475a-bb90-b895e27b8e50', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c5192e77-d845-4780-9f13-4944bb6fbdc7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c51b6d07-f61a-4dfc-935e-b0956b3a272c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('c51bba3e-0753-4e10-a5a4-3c1b32103954', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('c5358292-3337-4bf8-b0f1-204a180b8a6c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('c538fd2d-1a42-4102-80d0-10ccca646661', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('c54ffcc8-b4e8-4e19-94a6-a6565580d3ac', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('c556f35e-cc9d-4eca-b056-46e09a522be5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('c5e1a998-e4f0-4070-bc88-8eecd1497b6b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('c5e6e306-ae9c-476a-bc53-ab7935ae3ac6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('c678cdb5-373c-481b-ac85-0bcbc7bfa859', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('c683bb87-554d-4aa1-bdaf-9d7c0b1d5c99', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('c6e5b769-9bcc-4d2e-a410-880376c254b1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('c6eb9e64-7c66-446f-8f51-2f4c2f366d24', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('c72cb4e1-16eb-4024-9bce-0b36c99da09e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('c785484e-a0c7-440b-b7cf-abb33c731d07', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('c7b99e21-7e96-4c29-a1f2-3f844643fed7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('c81af753-4f5d-455e-b9a3-aee6bcc3c822', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('c8475649-4d6e-4b72-90d0-3866eb455177', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('c8b84427-9b31-409c-ab38-8a4f62fea550', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('c91ad5e7-433b-492e-9a49-70f530408c9b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('c92dc9ce-040b-4e1d-a046-1f567578341f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('c95439e2-8fb9-4856-92a9-239aed67ba85', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('c9560e73-0ca2-4e1a-81d5-644c2a9c6bb9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('c9a0013b-932f-41c1-8e35-3dd02a41da68', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('c9b57a05-56c2-4adc-86bd-5d237aad75d6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('ca0661cf-7375-474a-818f-d9b69e8e49ef', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('cb09db66-5038-4a91-9ae2-a725db2d592a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('cb5844d8-8048-4527-9c3c-4298b13fb1ac', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('cbda0710-7e90-4272-8d66-586bd9804bea', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('cbf3ff4c-699f-4b96-b780-429885db989b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:01'),
('cc53c4e1-06b2-4941-991d-26463f850351', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('cc682b4f-2122-4c2d-aaa2-ef21336c6f64', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('cc8c4b0c-6a30-428e-ad78-405a8dc1ee0b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('cca247fe-cc43-4054-b6a5-14933ea633c9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('cce0120e-3d0f-4c5d-a55b-c88cca75bd01', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 100, 'Skid', 10.00, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('cce8a8d5-8516-4333-9bc3-dc462a554d2e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('cce9e5d9-3f72-4987-9046-0eda8a876127', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('ccf1d584-2fbf-449e-8faa-956f21a29da3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('ccf3d688-f97e-4e1a-99df-796a8b0eca0f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('cd12774d-d9fa-4be2-a787-c09b0eddd5a1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('cd2ed471-e873-4b46-8f5a-f00996b76788', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('cd7cdbb3-32cd-469c-8a59-ae83f7ba4e0b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('ce2d0b10-2a89-498a-a9ff-26e3e07a97b4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('ce7283a1-f8f0-459e-b1ea-b65b5285650b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('cead57da-c234-47df-8134-38dee54d0e6b', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('cecec231-7c12-4120-9d52-1bbfebedd771', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('cf0a12f2-40c8-4dd6-b39e-c9fc5fd7957d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('cf52838a-f9d4-4e1f-a712-c8ef9c144c0b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('cf5877ed-fa55-4746-91d4-28d0bb846708', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('cfd17cda-7cc9-4680-85df-e795c48f0985', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('cff96ee5-d4d0-4b29-8740-6b8a427d9ed9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('d0b1012d-ea47-4f4a-99ef-f6a68fdfe357', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('d0f799e6-f933-4113-afc8-77c4b14e23d3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('d11b5c74-5585-4750-b623-4e3fdef15745', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('d1d7137d-78c8-4bdd-acb8-8b0dd2e9f4ae', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('d2385c40-9d43-49e3-99c1-2ae17458ef8c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('d3374b32-d048-4301-95d1-55661e02a597', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('d34545d9-293b-47d8-9ce1-16b1c94b4e57', 'PART-001', 'Steel Coil', 'PO12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('d3a8f137-b834-4a33-ac8e-063a67453602', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('d3c5c8a7-d61d-4046-9436-3e1ab2766572', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('d4a019b1-9586-446d-a314-799262261121', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('d52f03cc-32a8-4b6a-adc7-ce229b6692e2', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('d59f405c-1f01-42a1-b262-59bb475deb61', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('d5e82abe-f299-4a9f-ba12-99bfbe40c7ad', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('d672703a-7286-40cd-a34d-eba4c35e2e01', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('d769aecc-f533-4fee-8bb2-3da11aa05f3c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('d7da3265-321a-4e88-89ea-721d0b1b904f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('d81c1f26-c2cc-45ef-9706-4a6fc8c3ad0c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('d84d4663-7d60-4eea-8b63-aa09170d63cb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('d87ac531-35bd-4dc0-970f-d6ed94b37933', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('d89f30ab-831b-4a91-929c-96da87b68960', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('d8ab73c6-9921-464d-b3fb-70d485281c09', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('d8e7ce43-3ec4-4969-9d4f-6481bcdb7dc9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('d9813254-b08b-4436-a556-4fec23c03eff', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('d981c40f-c8d7-4513-a2b2-f36ca9bc7965', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:06'),
('d9ac7bf8-88e0-4047-9094-191580680902', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('d9bac347-17d1-42c4-bfff-c98cbe77fce8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('d9d940d2-ca01-4c92-b60c-e877abe3c5cf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('da4b87e6-dca5-4fab-b481-614b739665a2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('dadf5677-9ba5-480e-bd08-d6c681f77940', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('db7fea43-17cc-45d8-b6b9-f221e0dc8e6e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('db8e86ae-db10-472d-b6c7-3649f014bdf3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('dbb00bd0-2d06-4ce8-918a-4148b2865b10', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('dbc06085-dd5a-4fb1-9239-ae44f1ba142c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('dcab8897-df2b-4e6f-9e21-4fc48eb82bb2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('ddbdf71f-5149-46f8-9a23-799604f6b044', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('de3c76c4-4f4d-4098-94f9-b9cfd3c011a5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('de43bde4-403c-444e-9d02-677a01940941', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('deb00ef5-4c28-4e5b-97b1-b70e98274bea', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('debecb97-d09d-4c22-b247-c4bcf3d3d57c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('df15ed7f-b4ab-4fe5-9db5-927565cbf635', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('df6eeaef-a8a9-4333-91cf-2b48f5d66c62', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1000, 'Skid', 1.00, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('df743d23-e390-4b8f-b9f4-8f77a27ae2b3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('df8a3382-e379-482b-b469-0bb1e03fd6e0', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('dfa2527f-cac2-4a33-804c-be01576ddc8c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('dff7a7a0-d3a7-4468-9603-e946b6f57dd4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('e0740ed5-87a6-4339-93dc-fd12f853d9df', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e0ce5060-9476-470b-9eac-44be07f3f499', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('e0dd3dbe-cc94-4d89-8d62-b9e15e5a6594', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e140420f-05b0-42a3-ba11-8fcfa4f4c9fa', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('e1421209-1d21-4f20-a081-af5c9bb20310', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e16c71bf-0c75-495b-b763-a7e9ddf23566', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('e1abf3fb-e3c3-43ea-b356-171cfc7a7a82', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('e1e01eee-c3d0-4ea3-8086-8446ff5d43b2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('e2ec97b4-fbee-4b41-adac-c2fdbee1d79c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('e2ed0eb1-e5b2-44b7-8322-6d88d98a5329', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('e31ecb33-e7d4-4076-8943-71182b64b07a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('e3273d3c-e6de-40cd-85f7-79820796a4e4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('e347491f-a953-4fd4-809c-d47d5bd0588d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('e3e1a85d-37e0-4a2c-9fec-50b312e05f7b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('e427a293-c4fa-44de-a685-a262d81710c5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('e4517348-637d-4629-ad7f-ce71a24be345', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('e4658b46-ccda-4094-b8f2-567bfddac00a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('e4908db0-5311-43c0-bad3-97892d28fdfa', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('e54cb437-91c4-49f1-a3bc-b2987d4960cd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('e5533488-3583-4f79-9854-5b5b92708fbe', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e5c91cb3-d47f-4f7d-aafb-763638475127', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('e6a1e8d7-0c02-4e38-a6f3-0e87fcfa1eff', 'PART-001', 'Steel Coil', '12345', '001', 1, 0.00, 'HEAT-ABC123', 10, 'Skid', 0.00, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('e6e10c55-6875-4b08-a3a0-31aee967472a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e720c293-83b3-4270-83b4-e9215ed8c7d8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('e7c0c528-317f-425c-8acf-d8fe8fa1c34b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('e810cb64-8599-437d-84f9-82bc07c3fef7', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('e815d4dd-21e6-4487-858e-d51675e5068e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('e82ec278-b8d4-415b-92db-10d105f3428c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('e846c39d-a046-4601-ae97-1c9127b1e6ea', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('e86ab3b5-3e5c-4444-88a0-2a5a623268c3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e877be53-ebb3-4176-ac85-96498cee8b37', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e8925de1-2481-49b8-9d59-c53cccf0dd78', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('e8b3a610-49a9-4835-bf1e-da2cda938a03', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('e8e901bd-d936-4aa4-a22c-631d27ad531f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('e9d9c7c7-b12d-43f6-856c-6d989f4b8669', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('ea34eb50-df61-4f37-8833-2298bf687667', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('ea7043ac-3224-44e9-8d2a-b1312b557510', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('ea7d7044-ec0c-4e97-a6be-b3e9cbb453e5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('eacc8fc0-e46c-4995-8946-38c26e0ae374', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('eba82610-c94f-43fc-b050-70a933bc3493', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('ebb1eb4d-5f91-4232-87ff-4cb52aa48c1f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('ebde261d-2242-4b87-8383-46a6d0c4703c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:56', '2026-01-13 18:32:55'),
('ebe101cb-6b30-4572-a3b6-86338ca7fe3c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('ec3a0147-0de2-4f27-840f-94f893cd0292', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('ec790fb8-327e-40a6-963f-d0bae0888490', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('ecfaf169-701e-4608-91e5-a844dd11f5cf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('ed0c2fe2-ec6e-4e3a-b1be-ecb15f4f340b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('ed20ce06-4241-4a4e-b63c-2aab3493ead6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('ed4abcc9-15ab-42f9-9311-47676661ede4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:40:06', '2026-01-13 18:40:05'),
('eda69f9d-28bb-4754-ac9d-ee9a93a4a87d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('edc48477-c6d2-45e8-ba43-7d4fd3fb69f8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('ede5622b-85ef-44ba-b3f2-72fe08ef5954', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('edeacf15-c8dc-4967-b64d-b9b7563907ad', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('ee177ebd-6ed1-4a91-96df-46a99af81e96', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('ee8ba7aa-7eaf-4886-922b-1692484a9ed9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('eeca2775-6a90-4640-9c92-f23af9f0f0f8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('ef33aea9-af3c-4e42-a29d-ca3309a90bcc', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('efbfed66-a293-41c6-97dd-001b2aa4bf8c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('efea0050-f503-4738-8b31-75023772c44b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('f0479314-bec2-4b00-8e11-031d1f404521', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f09c8ca9-a01d-45d2-892a-faf015d6a431', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('f0ad8a18-2e64-494f-afc5-20ff9a06736b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('f0b0b9fc-637f-4b11-9e2f-42348981ca80', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f0fd0a33-00e8-4210-972a-79ca47fce520', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('f13b3f6c-4972-45da-bc1a-99b61ded8e23', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('f1a906ab-aa87-4bcd-af78-522994d7aecf', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('f1b4cea3-64f7-4e69-84bb-4c7a196c85f7', 'PART-@#$%', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-<>{}', 10, 'Package\'s \"Type\"', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('f1c94f6d-fa6c-4a1a-a97a-6fdc24e6e21c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f222c0fb-1633-43e5-9fb9-e118282288f1', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f25dc88f-6f94-4e38-8505-6383757139dd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('f26b27f0-5b59-4ff0-9831-1d050bb46467', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('f29c3615-ce70-4843-b32e-a71c4ec19ca0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('f2b4c36f-5c35-4ffd-9342-69c8e07d367b', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('f2bc6bd6-6c06-4565-91c4-1607a0b60a28', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:48:13', '2026-01-13 18:48:12'),
('f2c91264-9622-432c-8412-ed253b952e7f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('f336c22a-0275-4ceb-90aa-75ed80c92bd0', 'PART-001', 'Steel Coil', '12345', '001', 1, 1.50, 'HEAT-ABC123', 10, 'Skid', 0.15, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('f49063a7-0fcf-44a6-88d3-d11fae7a693a', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('f4f80c3b-6a3a-4bd5-8204-fd85297f6a9e', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000000.99, 'HEAT-ABC123', 10, 'Skid', 100000.10, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('f531f56c-1388-40af-a6d5-04fd87a053be', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('f57639d9-217c-4fad-aad7-12c2deff10a3', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('f62ecd98-e8b8-4955-ac44-36a5a1e833ff', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('f68752df-732a-49d3-a3fb-a2d94343695f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('f6b4c456-56bc-4f79-8282-8fcd7ad40885', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('f6ec6e1e-3eb0-4a34-a2f0-b3d409ffa3e2', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f6f8d9a9-ed77-4bd8-96f7-c3e0261a356c', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('f70fbf8e-63f6-47b0-884d-20ee369305ed', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('f7955271-278f-4cf0-b39b-fbeaf854e2cb', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f7b11326-44f4-4a5a-ab0b-030d9dc21ae5', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 1, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f9009d67-cb32-427d-8dd9-6e550cea5ae6', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56'),
('f949c741-28d4-40e4-9ed7-959fcc545f37', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('faaf26fe-905f-49ea-9079-c576fcfe6c5f', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('faf6f5df-0196-439f-b8c0-226560e2f17d', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('fb5ff946-58c2-4c00-b302-891382924666', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('fbb409c9-590e-49eb-9dae-cc9d36fc6aed', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('fbd99304-f4ed-4e5c-bf80-14ef71aa00cd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('fbf6ea1b-4370-4210-ab42-a2a4a2004acd', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('fc10b2f0-0888-4aa5-8161-797131de9718', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-12 08:45:46', '2026-01-12 14:45:46'),
('fc67b56a-9bc0-4854-bafa-e4147d9635f8', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:54:43', '2026-01-13 18:54:42'),
('fc932d44-4b2d-4453-babf-e7dceccf53f5', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('fcaa3cce-0552-49db-9b29-24b29cd50069', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:45:21', '2026-01-13 18:45:21'),
('fd1f8f33-ab8e-42a4-a540-4f01746a931d', 'PART-001', 'Steel Coil', NULL, '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('fd5065e5-cadd-4afd-902d-510881ef7a93', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:38:02', '2026-01-13 16:38:02'),
('fd535f53-32a3-4ad4-b126-66ba0199c742', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 10:40:33', '2026-01-13 16:40:32'),
('fe849df8-bbd6-441a-9da5-23f68899d384', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('fed04f73-208b-425c-8c1d-90ed019b83f4', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 1, 'Skid', 1000.00, 0, '2026-01-13 12:32:55', '2026-01-13 18:32:55'),
('fee29cec-dcf2-426d-8cae-39f9e16d6f83', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-16 20:08:38', '2026-01-17 02:08:37'),
('ffc19b8c-1451-4b67-a4c8-ac09dbbb75f9', 'PART-001', 'Steel Coil', '12345', '001', 1, 1000.50, 'HEAT-ABC123', 10, 'Skid', 100.05, 0, '2026-01-13 12:35:57', '2026-01-13 18:35:56');

DROP TABLE IF EXISTS `receiving_label_data`;
CREATE TABLE `receiving_label_data` (
  `id` int(11) NOT NULL COMMENT 'Primary key, auto-incrementing unique identifier',
  `quantity` int(11) NOT NULL COMMENT 'Quantity of parts received on this label',
  `part_id` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Part identifier from Infor Visual',
  `po_number` int(11) NOT NULL COMMENT 'Purchase order number from Infor Visual',
  `employee_number` int(11) NOT NULL COMMENT 'Employee ID who processed the receiving',
  `heat` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Heat/lot number for material traceability (optional)',
  `transaction_date` date NOT NULL COMMENT 'Date the receiving transaction occurred',
  `initial_location` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Initial warehouse location for received parts (optional)',
  `coils_on_skid` int(11) DEFAULT NULL COMMENT 'Number of coils on the skid (for coil materials, optional)',
  `label_number` int(11) DEFAULT '1' COMMENT 'Sequential label number when splitting quantities (default: 1)',
  `vendor_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Vendor/supplier name (optional, for reference)',
  `part_description` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Part description from Infor Visual (optional, for reference)',
  `created_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when record was created'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Receiving label data - stores information for each receiving label printed';

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
(1, 'MCC', 'Coils', 0, 1, 1, '2026-01-12 08:42:38', '2026-01-12 08:42:38', NULL),
(2, 'MMF', 'Sheets', 0, 2, 1, '2026-01-12 08:42:38', '2026-01-12 08:42:38', NULL),
(3, 'DEFAULT', 'Skids', 1, 99, 1, '2026-01-12 08:42:38', '2026-01-12 08:42:38', NULL);

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
DROP TABLE IF EXISTS `routing_history`;
CREATE TABLE `routing_history` (
  `id` int(11) NOT NULL COMMENT 'Primary key: unique history id',
  `label_id` int(11) NOT NULL COMMENT 'Foreign key to routing_label_data.id',
  `field_changed` varchar(50) NOT NULL COMMENT 'Name of the label field that was changed',
  `old_value` varchar(200) DEFAULT NULL COMMENT 'Previous value before the edit',
  `new_value` varchar(200) DEFAULT NULL COMMENT 'New value after the edit',
  `edited_by` int(11) NOT NULL COMMENT 'User id of the editor',
  `edit_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when the edit was made'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Label edit history for audit trail';

TRUNCATE TABLE `routing_history`;
DROP TABLE IF EXISTS `routing_home_locations`;
CREATE TABLE `routing_home_locations` (
  `id` int(11) NOT NULL COMMENT 'Primary key: unique identifier for the routing rule',
  `match_type` enum('Part Number','Vendor','PO Type','Part Category') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Type of pattern matching (e.g., Part Number, Vendor, PO Type, Part Category)',
  `pattern` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Wildcard pattern (e.g., VOL-*, *-BOLT)',
  `destination_location` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Target location code to route matched items to',
  `priority` int(11) DEFAULT '50' COMMENT 'Priority (1-100, lower values execute first)',
  `is_active` tinyint(1) DEFAULT '1' COMMENT 'Flag indicating if the rule is currently enabled/active',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Record last-updated timestamp',
  `created_by` int(11) DEFAULT NULL COMMENT 'FK toauth_users table (creator user id)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Auto-routing rules with pattern matching for routing module';

TRUNCATE TABLE `routing_home_locations`;
INSERT INTO `routing_home_locations` (`id`, `match_type`, `pattern`, `destination_location`, `priority`, `is_active`, `created_at`, `updated_at`, `created_by`) VALUES
(1, 'Part Number', 'VMB-*', 'VOLVO-VITS', 10, 1, '2026-01-12 08:42:38', '2026-01-12 08:42:38', NULL),
(2, 'Part Number', 'MMC-*', 'RECV', 20, 1, '2026-01-12 08:42:38', '2026-01-12 08:42:38', NULL),
(3, 'Part Number', 'MMF-*', 'T-00', 30, 1, '2026-01-12 08:42:38', '2026-01-12 08:42:38', NULL);

DROP TABLE IF EXISTS `routing_label_data`;
CREATE TABLE `routing_label_data` (
  `id` int(11) NOT NULL COMMENT 'Primary key - unique identifier for each routing label',
  `po_number` varchar(20) NOT NULL COMMENT 'Purchase order number associated with the label',
  `line_number` varchar(10) NOT NULL COMMENT 'PO line number associated with the label',
  `description` varchar(200) NOT NULL COMMENT 'Human-readable description for the label',
  `recipient_id` int(11) NOT NULL COMMENT 'FK to routing_recipients.id - intended recipient of the label',
  `quantity` int(11) NOT NULL COMMENT 'Quantity of items represented by this label',
  `created_by` int(11) NOT NULL COMMENT 'User id who created the label',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when the label was created',
  `other_reason_id` int(11) DEFAULT NULL COMMENT 'Optional FK to routing_po_alternatives.id for custom reasons',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Flag indicating whether the label is active (1) or inactive (0)',
  `csv_exported` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Flag indicating whether this label has been exported to CSV (1 = yes)',
  `csv_export_date` datetime DEFAULT NULL COMMENT 'Timestamp when the label was exported to CSV (nullable)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Routing labels primary table - stores label metadata and export state';

TRUNCATE TABLE `routing_label_data`;
DROP TABLE IF EXISTS `routing_po_alternatives`;
CREATE TABLE `routing_po_alternatives` (
  `id` int(11) NOT NULL COMMENT 'Primary surrogate key for the reasons table',
  `reason_code` varchar(20) NOT NULL COMMENT 'Short unique code identifying the non-PO reason',
  `description` varchar(200) NOT NULL COMMENT 'Human-readable description of the reason',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT '1 = active (available for selection); 0 = inactive',
  `display_order` int(11) NOT NULL DEFAULT '999' COMMENT 'Numeric ordering for UI display; lower values shown first'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Non-PO package reasons enumeration';

TRUNCATE TABLE `routing_po_alternatives`;
DROP TABLE IF EXISTS `routing_recipients`;
CREATE TABLE `routing_recipients` (
  `id` int(11) NOT NULL COMMENT 'Unique recipient identifier',
  `name` varchar(100) NOT NULL COMMENT 'Full name of the recipient',
  `location` varchar(100) NOT NULL COMMENT 'Physical location or building',
  `department` varchar(100) DEFAULT NULL COMMENT 'Department or organizational unit',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Active status (1=active, 0=inactive)',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `updated_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Last modification timestamp'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Package recipients master table';

TRUNCATE TABLE `routing_recipients`;
DROP TABLE IF EXISTS `routing_recipient_tracker`;
CREATE TABLE `routing_recipient_tracker` (
  `id` int(11) NOT NULL COMMENT 'Primary key - surrogate id',
  `employee_number` int(11) NOT NULL COMMENT 'Employee number (MTM employee identifier)',
  `recipient_id` int(11) NOT NULL COMMENT 'Foreign key referencing routing_recipients.id',
  `usage_count` int(11) NOT NULL DEFAULT '0' COMMENT 'Count of times employee selected this recipient',
  `last_used_date` datetime NOT NULL COMMENT 'Last datetime when the recipient was used by the employee'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Employee-recipient usage tracking for personalization';

TRUNCATE TABLE `routing_recipient_tracker`;
DROP TABLE IF EXISTS `settings_activity`;
CREATE TABLE `settings_activity` (
  `id` int(11) NOT NULL COMMENT 'Primary key for the audit record',
  `setting_id` int(11) NOT NULL COMMENT 'FK to settings_universal(id)',
  `user_setting_id` int(11) DEFAULT NULL COMMENT 'FK to settings_personal(id) if a user-specific override was changed',
  `old_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Previous value (serialized as stored; e.g., JSON or plain text)',
  `new_value` text COLLATE utf8mb4_unicode_ci COMMENT 'New value (serialized as stored; e.g., JSON or plain text)',
  `change_type` enum('create','update','delete','lock','unlock','reset') COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Nature of the change: create/update/delete/lock/unlock/reset',
  `changed_by` int(11) NOT NULL COMMENT 'FK to users(id) - user who performed the change',
  `changed_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'When the change occurred (server time)',
  `ip_address` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'IP address of the user or system performing the change (IPv4/IPv6)',
  `workstation_name` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Client workstation or host name where change originated'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Audit trail for all settings changes';

TRUNCATE TABLE `settings_activity`;
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
  `user_id` int(11) NOT NULL COMMENT 'FK to users.id - identifies the user owning this override',
  `setting_id` int(11) NOT NULL COMMENT 'FK to settings_universal.id - references the system setting being overridden',
  `setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'User-specific override value for the referenced setting',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Timestamp when the record was created',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Timestamp when the record was last updated'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='User-specific setting overrides';

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
DROP TABLE IF EXISTS `settings_routing_personal`;
CREATE TABLE `settings_routing_personal` (
  `id` int(11) NOT NULL COMMENT 'Surrogate PK for user preference record',
  `employee_number` int(11) NOT NULL COMMENT 'Employee number (owner of these preferences)',
  `default_mode` varchar(20) NOT NULL DEFAULT 'WIZARD' COMMENT 'Default routing mode (e.g., WIZARD, MANUAL)',
  `enable_validation` tinyint(1) NOT NULL DEFAULT '1' COMMENT 'Flag (1=true,0=false) to enable routing validation checks',
  `updated_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Last updated timestamp (auto-managed)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='User preferences for routing module';

TRUNCATE TABLE `settings_routing_personal`;
DROP TABLE IF EXISTS `settings_universal`;
CREATE TABLE `settings_universal` (
  `id` int(11) NOT NULL COMMENT 'Auto-incrementing primary key for the setting record',
  `category` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Settings category (System, Security, Receiving, Dunnage, Routing, Volvo, Reporting, ERP, UserDefaults)',
  `sub_category` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT 'Optional sub-category for hierarchical organization',
  `setting_key` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Unique key for the setting (machine identifier, used in code)',
  `setting_name` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Human-readable display name shown in UI',
  `description` text COLLATE utf8mb4_unicode_ci COMMENT 'Detailed description / help text shown toauth_users or administrators',
  `setting_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Current value (store JSON for complex types; plain text for simple types)',
  `default_value` text COLLATE utf8mb4_unicode_ci COMMENT 'Factory default value to fall back to when no user value is provided',
  `data_type` enum('string','int','boolean','json','path','password','email') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'string' COMMENT 'Data type hint for validation, UI rendering, and storage handling',
  `scope` enum('system','user') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'system' COMMENT 'Determines if the setting is system-wide or can be overridden per user',
  `permission_level` enum('user','operator','admin','developer','superadmin') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'admin' COMMENT 'Minimum permission level required to view/modify this setting',
  `is_locked` tinyint(1) DEFAULT '0' COMMENT 'If true, prevents modification even by higher-permission auth_users (administrative lock)',
  `is_sensitive` tinyint(1) DEFAULT '0' COMMENT 'If true, value should be treated as sensitive: encrypted at rest and masked in the UI',
  `validation_rules` json DEFAULT NULL COMMENT 'JSON object containing validation metadata (min, max, pattern, allowed_values, etc.)',
  `ui_control_type` enum('textbox','numberbox','toggleswitch','combobox','passwordbox','folderpicker','datagrid') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'textbox' COMMENT 'Preferred UI control type for editing this setting',
  `ui_order` int(11) DEFAULT '0' COMMENT 'Display order within category (lower values shown first)',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp (UTC recommended)',
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Record last updated timestamp (updated automatically)',
  `updated_by` int(11) DEFAULT NULL COMMENT 'FK toauth_users table - ID of the user who last updated the setting'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='System-wide application settings';

TRUNCATE TABLE `settings_universal`;DROP VIEW IF EXISTS `view_dunnage_history`;
CREATE TABLE `view_dunnage_history` (
`id` char(36)
,`dunnage_type` varchar(100)
,`part_number` varchar(50)
,`specs_combined` text
,`quantity` decimal(10,2)
,`created_date` date
,`employee_number` varchar(50)
,`source_module` varchar(7)
);
DROP VIEW IF EXISTS `view_receiving_history`;
CREATE TABLE `view_receiving_history` (
`id` char(36)
,`po_number` varchar(20)
,`part_number` varchar(50)
,`part_description` varchar(50)
,`quantity` decimal(10,2)
,`weight_lbs` decimal(10,2)
,`heat_lot_number` varchar(50)
,`created_date` date
,`employee_number` binary(0)
,`source_module` varchar(9)
);
DROP VIEW IF EXISTS `view_routing_history`;
CREATE TABLE `view_routing_history` (
`id` int(11)
,`po_number` varchar(20)
,`line_number` varchar(10)
,`package_description` varchar(200)
,`deliver_to` varchar(100)
,`department` varchar(100)
,`location` varchar(100)
,`quantity` int(11)
,`employee_number` int(11)
,`created_date` datetime
,`other_reason` varchar(200)
,`source_module` varchar(7)
);
DROP VIEW IF EXISTS `view_volvo_history`;
CREATE TABLE `view_volvo_history` (
`id` int(11)
,`shipment_number` int(11)
,`shipment_date` date
,`po_number` varchar(50)
,`receiver_number` varchar(50)
,`status` enum('pending_po','completed')
,`employee_number` varchar(20)
,`notes` text
,`part_count` bigint(21)
,`created_date` datetime
,`source_module` varchar(5)
);
DROP VIEW IF EXISTS `view_volvo_label_data_history`;
CREATE TABLE `view_volvo_label_data_history` (
`shipment_id` int(11)
,`shipment_date` date
,`shipment_number` int(11)
,`po_number` varchar(50)
,`receiver_number` varchar(50)
,`status` enum('pending_po','completed')
,`part_number` varchar(20)
,`received_skid_count` int(11)
,`calculated_piece_count` int(11)
,`has_discrepancy` tinyint(1)
,`expected_skid_count` int(11)
,`discrepancy_note` text
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

    
    IF NEW.status = 'pending_po' AND NEW.is_archived = 0 THEN

        
        
        SELECT COUNT(*) INTO pending_count
        FROM volvo_label_data
        WHERE status = 'pending_po'
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

    
    
    IF NEW.status = 'pending_po' AND NEW.is_archived = 0
       AND (OLD.status != 'pending_po' OR OLD.is_archived = 1) THEN

        
        SELECT COUNT(*) INTO pending_count
        FROM volvo_label_data
        WHERE status = 'pending_po'
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

DROP TABLE IF EXISTS `volvo_line_data`;
CREATE TABLE `volvo_line_data` (
  `id` int(11) NOT NULL COMMENT 'Primary key for volvo_line_data',
  `shipment_id` int(11) NOT NULL COMMENT 'FK to volvo_label_data.id (shipment header)',
  `part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'From volvo_masterdata',
  `quantity_per_skid` int(11) NOT NULL DEFAULT '0' COMMENT 'Cached quantity per skid from master data at time of shipment',
  `received_skid_count` int(11) NOT NULL COMMENT 'User-entered actual skid count',
  `calculated_piece_count` int(11) NOT NULL COMMENT 'Stored snapshot of calculated piece count from component explosion',
  `has_discrepancy` tinyint(1) NOT NULL DEFAULT '0' COMMENT 'Flag indicating mismatch between expected and received counts (0=false,1=true)',
  `expected_skid_count` int(11) DEFAULT NULL COMMENT 'Volvo packlist quantity if discrepancy exists (nullable)',
  `discrepancy_note` text COLLATE utf8mb4_unicode_ci COMMENT 'Optional note describing discrepancy or resolution steps'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Volvo shipment line items';

TRUNCATE TABLE `volvo_line_data`;
DROP TABLE IF EXISTS `volvo_masterdata`;
CREATE TABLE `volvo_masterdata` (
  `part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Volvo part identifier (primary key)',
  `quantity_per_skid` int(11) NOT NULL COMMENT 'Pieces per skid for this part (from DataSheet.csv)',
  `is_active` tinyint(1) NOT NULL DEFAULT '1' COMMENT '0=deactivated, hidden from dropdowns',
  `created_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Record creation timestamp',
  `modified_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Record last modified timestamp'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Volvo parts catalog (from DataSheet.csv)';

TRUNCATE TABLE `volvo_masterdata`;
DROP TABLE IF EXISTS `volvo_part_components`;
CREATE TABLE `volvo_part_components` (
  `id` int(11) NOT NULL COMMENT 'Surrogate primary key for this table',
  `parent_part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Parent part number (references volvo_masterdata.part_number)',
  `component_part_number` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Component part number (references volvo_masterdata.part_number)',
  `quantity` int(11) NOT NULL COMMENT 'How many of this component per parent skid'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Component explosion for Volvo parts';

TRUNCATE TABLE `volvo_part_components`;

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
  ADD KEY `FK_dunnage_history_part_id` (`part_id`);

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
  ADD PRIMARY KEY (`LoadID`),
  ADD KEY `idx_partid` (`PartID`),
  ADD KEY `idx_ponumber` (`PONumber`),
  ADD KEY `idx_receiveddate` (`ReceivedDate`),
  ADD KEY `idx_receiveddate_part` (`ReceivedDate`,`PartID`);

ALTER TABLE `receiving_label_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_part_id` (`part_id`) COMMENT 'Index for part lookup queries',
  ADD KEY `idx_po_number` (`po_number`) COMMENT 'Index for PO-based queries',
  ADD KEY `idx_transaction_date` (`transaction_date`) COMMENT 'Index for date range queries',
  ADD KEY `idx_employee_number` (`employee_number`) COMMENT 'Index for employee activity queries';

ALTER TABLE `receiving_package_types`
  ADD PRIMARY KEY (`PreferenceID`),
  ADD UNIQUE KEY `PartID` (`PartID`),
  ADD KEY `idx_partid` (`PartID`) COMMENT 'Fast lookup index for retrieving preferences by part ID';

ALTER TABLE `receiving_package_type_mapping`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_prefix` (`part_prefix`),
  ADD KEY `idx_active` (`is_active`);

ALTER TABLE `reporting_scheduled_reports`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_next_run` (`next_run_date`),
  ADD KEY `idx_active` (`is_active`),
  ADD KEY `idx_report_type` (`report_type`);

ALTER TABLE `routing_history`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_label_id` (`label_id`) COMMENT 'Index to quickly find history rows for a label',
  ADD KEY `idx_edit_date` (`edit_date`) COMMENT 'Index to query history by edit timestamp';

ALTER TABLE `routing_home_locations`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_pattern` (`match_type`,`pattern`),
  ADD KEY `idx_priority` (`priority`),
  ADD KEY `idx_active` (`is_active`);

ALTER TABLE `routing_label_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_po_number` (`po_number`),
  ADD KEY `idx_created_by` (`created_by`),
  ADD KEY `idx_created_date` (`created_date`),
  ADD KEY `idx_recipient_id` (`recipient_id`),
  ADD KEY `fk_label_other_reason` (`other_reason_id`);

ALTER TABLE `routing_po_alternatives`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `idx_unique_reason_code` (`reason_code`),
  ADD KEY `idx_display_order` (`display_order`);

ALTER TABLE `routing_recipients`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `idx_unique_name` (`name`) COMMENT 'Ensures unique recipient names',
  ADD KEY `idx_is_active` (`is_active`) COMMENT 'Optimizes queries filtering by active status';

ALTER TABLE `routing_recipient_tracker`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `idx_unique_employee_recipient` (`employee_number`,`recipient_id`) COMMENT 'Ensures single tracking row per employee/recipient pair',
  ADD KEY `idx_employee_number` (`employee_number`) COMMENT 'Index to quickly find usage by employee',
  ADD KEY `idx_recipient_id` (`recipient_id`) COMMENT 'Index to quickly find usage by recipient';

ALTER TABLE `settings_activity`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_setting` (`setting_id`),
  ADD KEY `idx_user` (`changed_by`),
  ADD KEY `idx_timestamp` (`changed_at`);

ALTER TABLE `settings_dunnage_personal`
  ADD PRIMARY KEY (`ID`),
  ADD UNIQUE KEY `IDX_PREF_001` (`UserId`,`PreferenceKey`) COMMENT 'One value per user per key';

ALTER TABLE `settings_module_volvo`
  ADD PRIMARY KEY (`setting_key`),
  ADD KEY `idx_category` (`category`);

ALTER TABLE `settings_personal`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_user_setting` (`user_id`,`setting_id`) COMMENT 'Ensures a single override per user per setting',
  ADD KEY `setting_id` (`setting_id`),
  ADD KEY `idx_user` (`user_id`) COMMENT 'Index to speed lookups by user_id';

ALTER TABLE `settings_personal_activity_log`
  ADD PRIMARY KEY (`log_id`),
  ADD KEY `idx_log_timestamp` (`event_timestamp`) COMMENT 'Index for querying logs by timestamp',
  ADD KEY `idx_log_username` (`username`) COMMENT 'Index for querying logs by username',
  ADD KEY `idx_log_event_type` (`event_type`) COMMENT 'Index for querying logs by event type';

ALTER TABLE `settings_routing_personal`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `idx_unique_employee` (`employee_number`);

ALTER TABLE `settings_universal`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_setting` (`category`,`setting_key`),
  ADD KEY `idx_category` (`category`),
  ADD KEY `idx_scope` (`scope`),
  ADD KEY `idx_permission` (`permission_level`);

ALTER TABLE `volvo_label_data`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `unique_shipment_per_day` (`shipment_date`,`shipment_number`) COMMENT 'Ensures shipment_number is unique per shipment_date',
  ADD KEY `idx_status` (`status`) COMMENT 'Index to speed queries filtered by status',
  ADD KEY `idx_shipment_date` (`shipment_date`) COMMENT 'Index to speed queries by shipment_date',
  ADD KEY `idx_po_number` (`po_number`) COMMENT 'Index to speed lookups by PO number';

ALTER TABLE `volvo_line_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_shipment_id` (`shipment_id`) COMMENT 'Index for lookup by shipment_id',
  ADD KEY `idx_part_number` (`part_number`) COMMENT 'Index for lookup by part_number';

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

ALTER TABLE `dunnage_parts`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for dunnage part';

ALTER TABLE `dunnage_requires_inventory`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for inventoried dunnage record';

ALTER TABLE `dunnage_specs`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for dunnage specifications';

ALTER TABLE `dunnage_types`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for dunnage type';

ALTER TABLE `receiving_label_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key, auto-incrementing unique identifier';

ALTER TABLE `receiving_package_types`
  MODIFY `PreferenceID` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each package type preference';

ALTER TABLE `receiving_package_type_mapping`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for mapping record', AUTO_INCREMENT=4;

ALTER TABLE `reporting_scheduled_reports`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key: unique scheduled report identifier';

ALTER TABLE `routing_history`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key: unique history id';

ALTER TABLE `routing_home_locations`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key: unique identifier for the routing rule', AUTO_INCREMENT=4;

ALTER TABLE `routing_label_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key - unique identifier for each routing label';

ALTER TABLE `routing_po_alternatives`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary surrogate key for the reasons table';

ALTER TABLE `routing_recipients`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique recipient identifier';

ALTER TABLE `routing_recipient_tracker`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key - surrogate id';

ALTER TABLE `settings_activity`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for the audit record';

ALTER TABLE `settings_dunnage_personal`
  MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each preference record';

ALTER TABLE `settings_personal`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for the settings_personal table';

ALTER TABLE `settings_personal_activity_log`
  MODIFY `log_id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Unique identifier for each log entry';

ALTER TABLE `settings_routing_personal`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate PK for user preference record';

ALTER TABLE `settings_universal`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Auto-incrementing primary key for the setting record';

ALTER TABLE `volvo_label_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key for shipment record';

ALTER TABLE `volvo_line_data`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Primary key for volvo_line_data';

ALTER TABLE `volvo_part_components`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Surrogate primary key for this table';
DROP TABLE IF EXISTS `view_dunnage_history`;

DROP VIEW IF EXISTS `view_dunnage_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_dunnage_history`  AS SELECT `dl`.`load_uuid` AS `id`, `dt`.`type_name` AS `dunnage_type`, `dp`.`part_id` AS `part_number`, group_concat(concat(`ds`.`spec_key`,':',coalesce(json_unquote(`ds`.`spec_value`),'')) order by `ds`.`spec_key` ASC separator ', ') AS `specs_combined`, `dl`.`quantity` AS `quantity`, cast(`dl`.`received_date` as date) AS `created_date`, `dl`.`created_by` AS `employee_number`, 'Dunnage' AS `source_module` FROM (((`dunnage_history` `dl` join `dunnage_parts` `dp` on((`dl`.`part_id` = `dp`.`part_id`))) join `dunnage_types` `dt` on((`dp`.`type_id` = `dt`.`id`))) left join `dunnage_specs` `ds` on((`dp`.`type_id` = `ds`.`type_id`))) GROUP BY `dl`.`load_uuid`, `dt`.`type_name`, `dp`.`part_id`, `dl`.`quantity`, `dl`.`received_date`, `dl`.`created_by` ORDER BY `dl`.`received_date` DESC ;
DROP TABLE IF EXISTS `view_receiving_history`;

DROP VIEW IF EXISTS `view_receiving_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_receiving_history`  AS SELECT `receiving_history`.`LoadID` AS `id`, `receiving_history`.`PONumber` AS `po_number`, `receiving_history`.`PartID` AS `part_number`, `receiving_history`.`PartType` AS `part_description`, `receiving_history`.`WeightQuantity` AS `quantity`, `receiving_history`.`WeightPerPackage` AS `weight_lbs`, `receiving_history`.`HeatLotNumber` AS `heat_lot_number`, cast(`receiving_history`.`ReceivedDate` as date) AS `created_date`, NULL AS `employee_number`, 'Receiving' AS `source_module` FROM `receiving_history` ORDER BY `receiving_history`.`ReceivedDate` DESC, `receiving_history`.`LoadID` DESC ;
DROP TABLE IF EXISTS `view_routing_history`;

DROP VIEW IF EXISTS `view_routing_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_routing_history`  AS SELECT `rl`.`id` AS `id`, `rl`.`po_number` AS `po_number`, `rl`.`line_number` AS `line_number`, `rl`.`description` AS `package_description`, `rr`.`name` AS `deliver_to`, `rr`.`department` AS `department`, `rr`.`location` AS `location`, `rl`.`quantity` AS `quantity`, `rl`.`created_by` AS `employee_number`, `rl`.`created_date` AS `created_date`, (case when (`rl`.`other_reason_id` is not null) then `ror`.`description` else NULL end) AS `other_reason`, 'Routing' AS `source_module` FROM ((`routing_label_data` `rl` join `routing_recipients` `rr` on((`rl`.`recipient_id` = `rr`.`id`))) left join `routing_po_alternatives` `ror` on((`rl`.`other_reason_id` = `ror`.`id`))) WHERE (`rl`.`is_active` = 1) ORDER BY `rl`.`created_date` DESC, `rl`.`id` DESC ;
DROP TABLE IF EXISTS `view_volvo_history`;

DROP VIEW IF EXISTS `view_volvo_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_volvo_history`  AS SELECT `vs`.`id` AS `id`, `vs`.`shipment_number` AS `shipment_number`, `vs`.`shipment_date` AS `shipment_date`, `vs`.`po_number` AS `po_number`, `vs`.`receiver_number` AS `receiver_number`, `vs`.`status` AS `status`, `vs`.`employee_number` AS `employee_number`, `vs`.`notes` AS `notes`, count(`vsl`.`id`) AS `part_count`, `vs`.`created_date` AS `created_date`, 'Volvo' AS `source_module` FROM (`volvo_label_data` `vs` left join `volvo_line_data` `vsl` on((`vs`.`id` = `vsl`.`shipment_id`))) GROUP BY `vs`.`id`, `vs`.`shipment_number`, `vs`.`shipment_date`, `vs`.`po_number`, `vs`.`receiver_number`, `vs`.`status`, `vs`.`employee_number`, `vs`.`notes`, `vs`.`created_date` ORDER BY `vs`.`created_date` DESC ;
DROP TABLE IF EXISTS `view_volvo_label_data_history`;

DROP VIEW IF EXISTS `view_volvo_label_data_history`;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `view_volvo_label_data_history`  AS SELECT `s`.`id` AS `shipment_id`, `s`.`shipment_date` AS `shipment_date`, `s`.`shipment_number` AS `shipment_number`, `s`.`po_number` AS `po_number`, `s`.`receiver_number` AS `receiver_number`, `s`.`status` AS `status`, `l`.`part_number` AS `part_number`, `l`.`received_skid_count` AS `received_skid_count`, `l`.`calculated_piece_count` AS `calculated_piece_count`, `l`.`has_discrepancy` AS `has_discrepancy`, `l`.`expected_skid_count` AS `expected_skid_count`, `l`.`discrepancy_note` AS `discrepancy_note` FROM (`volvo_label_data` `s` left join `volvo_line_data` `l` on((`s`.`id` = `l`.`shipment_id`))) ORDER BY `s`.`shipment_date` DESC, `s`.`shipment_number` DESC ;


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

ALTER TABLE `routing_history`
  ADD CONSTRAINT `fk_history_label` FOREIGN KEY (`label_id`) REFERENCES `routing_label_data` (`id`);

ALTER TABLE `routing_label_data`
  ADD CONSTRAINT `fk_label_other_reason` FOREIGN KEY (`other_reason_id`) REFERENCES `routing_po_alternatives` (`id`),
  ADD CONSTRAINT `fk_label_recipient` FOREIGN KEY (`recipient_id`) REFERENCES `routing_recipients` (`id`);

ALTER TABLE `routing_recipient_tracker`
  ADD CONSTRAINT `fk_usage_recipient` FOREIGN KEY (`recipient_id`) REFERENCES `routing_recipients` (`id`);

ALTER TABLE `settings_activity`
  ADD CONSTRAINT `settings_activity_ibfk_1` FOREIGN KEY (`setting_id`) REFERENCES `settings_universal` (`id`) ON DELETE CASCADE;

ALTER TABLE `settings_personal`
  ADD CONSTRAINT `settings_personal_ibfk_1` FOREIGN KEY (`setting_id`) REFERENCES `settings_universal` (`id`) ON DELETE CASCADE;

ALTER TABLE `volvo_line_data`
  ADD CONSTRAINT `volvo_line_data_ibfk_1` FOREIGN KEY (`shipment_id`) REFERENCES `volvo_label_data` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `volvo_line_data_ibfk_2` FOREIGN KEY (`part_number`) REFERENCES `volvo_masterdata` (`part_number`);

ALTER TABLE `volvo_part_components`
  ADD CONSTRAINT `fk_volvo_part_components_component` FOREIGN KEY (`component_part_number`) REFERENCES `volvo_masterdata` (`part_number`),
  ADD CONSTRAINT `fk_volvo_part_components_parent` FOREIGN KEY (`parent_part_number`) REFERENCES `volvo_masterdata` (`part_number`) ON DELETE CASCADE;

SET FOREIGN_KEY_CHECKS=1;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
