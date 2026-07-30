-- Validate devtools request type schema artifacts
-- Engine: MySQL 5.7

USE mtm_waitlist;

SELECT
    issue_type,
    object_name,
    expected_value,
    actual_value
FROM (
        SELECT 'missing_table' AS issue_type, 'ops_request_types_catalog' AS object_name, 'table exists' AS expected_value, 'missing' AS actual_value
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'ops_request_types_catalog'
        )
        UNION ALL
        SELECT 'missing_table', 'ops_request_types_card_fields', 'table exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'ops_request_types_card_fields'
        )
        UNION ALL
        SELECT 'missing_table', 'ops_request_types_detail_fields', 'table exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND table_name = 'ops_request_types_detail_fields'
        )
        UNION ALL
        SELECT 'missing_function', 'fn_ops_request_type_data_type_is_valid', 'function exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.routines
            WHERE routine_schema = DATABASE()
              AND routine_name = 'fn_ops_request_type_data_type_is_valid'
              AND routine_type = 'FUNCTION'
        )
        UNION ALL
        SELECT 'missing_procedure', 'sp_ops_request_types_create', 'procedure exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.routines
            WHERE routine_schema = DATABASE()
              AND routine_name = 'sp_ops_request_types_create'
              AND routine_type = 'PROCEDURE'
        )
        UNION ALL
        SELECT 'missing_procedure', 'sp_ops_request_type_card_fields_create', 'procedure exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.routines
            WHERE routine_schema = DATABASE()
              AND routine_name = 'sp_ops_request_type_card_fields_create'
              AND routine_type = 'PROCEDURE'
        )
        UNION ALL
        SELECT 'missing_procedure', 'sp_ops_request_type_detail_fields_create', 'procedure exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.routines
            WHERE routine_schema = DATABASE()
              AND routine_name = 'sp_ops_request_type_detail_fields_create'
              AND routine_type = 'PROCEDURE'
        )
        UNION ALL
        SELECT 'missing_view', 'vw_ops_request_types_summary', 'view exists', 'missing'
        WHERE NOT EXISTS (
            SELECT 1
            FROM information_schema.views
            WHERE table_schema = DATABASE()
              AND table_name = 'vw_ops_request_types_summary'
        )
    ) issues;
