-- Create table: ops_request_types_card_fields
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS ops_request_types_card_fields (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    request_type_id BIGINT NOT NULL,
    field_name VARCHAR(128) NOT NULL,
    field_name_normalized VARCHAR(128) NOT NULL,
    data_type_name VARCHAR(64) NOT NULL,
    display_order INT NOT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_ops_req_type_card_fields_public_id (public_id),
    UNIQUE KEY uq_ops_req_type_card_fields_req_type_field_name (request_type_id, field_name_normalized),
    KEY idx_ops_req_type_card_fields_req_type_display_order (request_type_id, display_order),
    CONSTRAINT fk_ops_req_type_card_fields_req_type_id
        FOREIGN KEY (request_type_id) REFERENCES ops_request_types_catalog (id)
        ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
