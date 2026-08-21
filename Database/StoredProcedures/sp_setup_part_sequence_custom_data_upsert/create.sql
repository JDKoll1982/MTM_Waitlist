-- Stored Procedure: sp_setup_part_sequence_custom_data_upsert
-- Engine: MySQL 5.7
-- Purpose: Upsert custom setup data for an exact part/sequence pair.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_setup_part_sequence_custom_data_upsert;

CREATE PROCEDURE sp_setup_part_sequence_custom_data_upsert(
    IN p_part_number VARCHAR(64),
    IN p_sequence_number VARCHAR(32),
    IN p_selected_scrap_type VARCHAR(128),
    IN p_selected_dunnage_type_id VARCHAR(64),
    IN p_selected_dunnage_part_id VARCHAR(64),
    IN p_subordinate_parts_json JSON,
    IN p_selected_dunnage_parts_json JSON,
    IN p_updated_by_user_id BIGINT
)
INSERT INTO setup_part_sequence_custom_data (
    public_id,
    part_number,
    sequence_number,
    selected_scrap_type,
    selected_dunnage_type_id,
    selected_dunnage_part_id,
    subordinate_parts_json,
    selected_dunnage_parts_json,
    created_by_user_id,
    updated_by_user_id,
    created_utc,
    updated_utc
)
SELECT
    UUID(),
    TRIM(p_part_number),
    TRIM(p_sequence_number),
    NULLIF(TRIM(p_selected_scrap_type) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(p_selected_dunnage_type_id) COLLATE utf8mb4_unicode_ci, ''),
    NULLIF(TRIM(p_selected_dunnage_part_id) COLLATE utf8mb4_unicode_ci, ''),
    p_subordinate_parts_json,
    p_selected_dunnage_parts_json,
    p_updated_by_user_id,
    p_updated_by_user_id,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
FROM dual
ON DUPLICATE KEY UPDATE
    selected_scrap_type = VALUES(selected_scrap_type),
    selected_dunnage_type_id = VALUES(selected_dunnage_type_id),
    selected_dunnage_part_id = VALUES(selected_dunnage_part_id),
    subordinate_parts_json = VALUES(subordinate_parts_json),
    selected_dunnage_parts_json = VALUES(selected_dunnage_parts_json),
    updated_by_user_id = VALUES(updated_by_user_id),
    updated_utc = UTC_TIMESTAMP();
