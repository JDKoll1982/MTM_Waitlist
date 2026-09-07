-- Create procedure: sp_mock_master_tables_registry_insert
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_insert;

CREATE PROCEDURE sp_mock_master_tables_registry_insert(
    IN p_table_name VARCHAR(128),
    IN p_ui_display_name VARCHAR(128),
    IN p_description_text VARCHAR(500),
    IN p_sort_rank INT
)
INSERT INTO mock_master_tables_registry (table_name, ui_display_name, description_text, sort_rank, public_id, is_active, created_utc, updated_utc)
VALUES (NULLIF(TRIM(p_table_name), ''), NULLIF(TRIM(p_ui_display_name), ''), NULLIF(TRIM(p_description_text), ''), p_sort_rank, UUID(), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

