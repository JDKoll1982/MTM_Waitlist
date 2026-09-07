-- Create procedure: sp_mock_master_tables_registry_update
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_update;

CREATE PROCEDURE sp_mock_master_tables_registry_update(
    IN p_id BIGINT,
    IN p_table_name VARCHAR(128),
    IN p_ui_display_name VARCHAR(128),
    IN p_description_text VARCHAR(500),
    IN p_sort_rank INT
)
UPDATE mock_master_tables_registry
SET table_name = NULLIF(TRIM(p_table_name), ''), ui_display_name = NULLIF(TRIM(p_ui_display_name), ''), description_text = NULLIF(TRIM(p_description_text), ''), sort_rank = p_sort_rank, updated_utc = UTC_TIMESTAMP()
WHERE id = p_id;

