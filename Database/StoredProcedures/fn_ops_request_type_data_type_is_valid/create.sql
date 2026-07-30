-- Validate request field data type value
-- Engine: MySQL 5.7

USE mtm_waitlist;

DROP FUNCTION IF EXISTS fn_ops_request_type_data_type_is_valid;

CREATE FUNCTION fn_ops_request_type_data_type_is_valid(p_data_type_name VARCHAR(64))
RETURNS TINYINT(1)
DETERMINISTIC
NO SQL
SQL SECURITY DEFINER
RETURN (
    CASE
        WHEN LOWER(TRIM(COALESCE(p_data_type_name, ''))) IN (
            'string',
            'int',
            'boolean',
            'list',
            'visual sql database queue',
            'mysql mtm_waitlist queue'
        ) THEN 1
        ELSE 0
    END
);
