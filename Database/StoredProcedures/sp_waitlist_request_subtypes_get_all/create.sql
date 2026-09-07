-- Create procedure: sp_waitlist_request_subtypes_get_all
-- Engine: MySQL 5.7
-- Purpose: Return ALL real (non-mock) request-subtype catalog rows INCLUDING inactive ones, for the
--          Developer/Admin editor (so an inactive subtype can be viewed, edited, and re-activated).
--          The wizard uses sp_waitlist_request_subtypes_get (active only); this is the admin variant.
--          Order is by request_type_id then id, which matches the seeded/display order.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_get_all;

CREATE PROCEDURE sp_waitlist_request_subtypes_get_all()
SELECT
    id,
    public_id,
    request_type_id,
    subtype_name,
    control,
    flow,
    requires_text_input,
    prompt_text,
    min_length,
    max_length,
    default_image_path,
    center_data_grid_fields_json,
    is_active,
    created_utc,
    updated_utc
FROM waitlist_request_subtypes
ORDER BY request_type_id ASC, id ASC;
