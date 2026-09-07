-- Create procedure: sp_mock_master_tables_registry_delete
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_tables_registry_delete;

CREATE PROCEDURE sp_mock_master_tables_registry_delete(IN p_id BIGINT)
DELETE FROM mock_master_tables_registry WHERE id = p_id;

