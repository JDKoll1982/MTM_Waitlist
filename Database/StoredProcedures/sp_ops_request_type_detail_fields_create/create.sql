-- Create procedure: sp_ops_request_type_detail_fields_create
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_request_type_detail_fields_create;

DELIMITER $$

CREATE PROCEDURE sp_ops_request_type_detail_fields_create(
    IN p_public_id CHAR(36),
    IN p_request_type_id BIGINT,
    IN p_field_name VARCHAR(128),
    IN p_field_name_normalized VARCHAR(128),
    IN p_data_type_name VARCHAR(64),
    IN p_display_order INT,
    IN p_created_utc DATETIME,
    IN p_updated_utc DATETIME
)
BEGIN
    IF fn_ops_request_type_data_type_is_valid(p_data_type_name) = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Unsupported data type for detail field.';
    END IF;

    INSERT INTO ops_request_types_detail_fields (
        public_id,
        request_type_id,
        field_name,
        field_name_normalized,
        data_type_name,
        display_order,
        created_utc,
        updated_utc
    )
    VALUES (
        p_public_id,
        p_request_type_id,
        p_field_name,
        p_field_name_normalized,
        p_data_type_name,
        p_display_order,
        p_created_utc,
        p_updated_utc
    );
END $$

DELIMITER ;
