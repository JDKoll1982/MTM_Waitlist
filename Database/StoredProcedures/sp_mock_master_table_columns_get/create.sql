-- Create procedure: sp_mock_master_table_columns_get
-- Engine: MySQL 5.7
-- Purpose: Return the editable-column metadata for a Developer-editable mock master table so the
--          editor can render an appropriate grid. Column names come from information_schema and are
--          never interpolated; the table name is validated against the known mock tables first.
-- NOTE: This SP does NOT cover the real (non-mock) request-type/subtype tables; they have their own
--       dedicated SPs + real-catalog service.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_mock_master_table_columns_get;

CREATE PROCEDURE sp_mock_master_table_columns_get(IN p_table_name VARCHAR(128))
SELECT
    c.COLUMN_NAME AS column_name,
    c.DATA_TYPE   AS data_type,
    c.IS_NULLABLE AS is_nullable,
    CASE WHEN c.COLUMN_NAME IN ('id','public_id','created_utc','updated_utc') THEN 1 ELSE 0 END AS is_audit
FROM information_schema.COLUMNS c
JOIN mock_master_tables_registry r
  ON r.table_name = c.TABLE_NAME
 AND r.is_active = 1
WHERE c.TABLE_SCHEMA = DATABASE()
  AND c.TABLE_NAME = TRIM(p_table_name)
ORDER BY c.ORDINAL_POSITION;
