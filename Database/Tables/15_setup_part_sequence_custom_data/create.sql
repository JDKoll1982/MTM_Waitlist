-- Create table: setup_part_sequence_custom_data
-- Engine: MySQL 5.7

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS setup_part_sequence_custom_data (
    id BIGINT NOT NULL AUTO_INCREMENT,
    public_id CHAR(36) NOT NULL,
    part_number VARCHAR(64) NOT NULL,
    sequence_number VARCHAR(32) NOT NULL,
    selected_scrap_type VARCHAR(128) NULL,
    selected_dunnage_type_id VARCHAR(64) NULL,
    selected_dunnage_part_id VARCHAR(64) NULL,
    subordinate_parts_json JSON NULL,
    selected_dunnage_parts_json JSON NULL,
    created_by_user_id BIGINT NULL,
    updated_by_user_id BIGINT NULL,
    created_utc DATETIME NOT NULL,
    updated_utc DATETIME NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_setup_part_sequence_custom_data_public_id (public_id),
    UNIQUE KEY uq_setup_part_sequence_custom_data_part_sequence (part_number, sequence_number),
    KEY idx_setup_part_sequence_custom_data_updated_utc (updated_utc)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;