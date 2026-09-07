-- Create procedure: sp_mock_master_tables_registry_get
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_get;

CREATE PROCEDURE sp_mock_master_tables_registry_get()
SELECT id, public_id, table_name, ui_display_name, description_text, sort_rank, is_active, created_utc, updated_utc
FROM mock_master_tables_registry
ORDER BY id ASC;

