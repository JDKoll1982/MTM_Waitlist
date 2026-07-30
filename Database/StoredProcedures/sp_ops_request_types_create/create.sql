-- Create procedure: sp_ops_request_types_create
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_ops_request_types_create;

DELIMITER $$

CREATE PROCEDURE sp_ops_request_types_create(
    IN p_public_id CHAR(36),
    IN p_request_type_name VARCHAR(128),
    IN p_request_type_name_normalized VARCHAR(128),
    IN p_image_file_path VARCHAR(512),
    IN p_created_by_username VARCHAR(128),
    IN p_created_utc DATETIME,
    IN p_updated_utc DATETIME,
    OUT p_request_type_id BIGINT
)
BEGIN
    INSERT INTO ops_request_types_catalog (
        public_id,
        request_type_name,
        request_type_name_normalized,
        image_file_path,
        created_by_username,
        is_active,
        created_utc,
        updated_utc
    )
    VALUES (
        p_public_id,
        p_request_type_name,
        p_request_type_name_normalized,
        p_image_file_path,
        p_created_by_username,
        1,
        p_created_utc,
        p_updated_utc
    );

    SET p_request_type_id = LAST_INSERT_ID();
END $$

DELIMITER ;
