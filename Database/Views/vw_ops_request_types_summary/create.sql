-- Create view: vw_ops_request_types_summary
-- Engine: MySQL 5.7

USE mtm_waitlist;

CREATE OR REPLACE VIEW vw_ops_request_types_summary AS
SELECT
    rt.id,
    rt.public_id,
    rt.request_type_name,
    rt.image_file_path,
    rt.created_by_username,
    rt.is_active,
    rt.created_utc,
    rt.updated_utc,
    (
        SELECT COUNT(1)
        FROM ops_request_types_card_fields cf
        WHERE cf.request_type_id = rt.id
    ) AS card_field_count,
    (
        SELECT COUNT(1)
        FROM ops_request_types_detail_fields df
        WHERE df.request_type_id = rt.id
    ) AS detail_field_count
FROM ops_request_types_catalog rt;
