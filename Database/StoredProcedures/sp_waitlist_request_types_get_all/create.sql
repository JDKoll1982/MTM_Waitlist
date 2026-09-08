-- Create procedure: sp_waitlist_request_types_get_all
-- Engine: MySQL 5.7
-- Purpose: Return ALL real (non-mock) request-type catalog rows INCLUDING inactive ones, for the
--          Developer/Admin editor (so an inactive type can be viewed, edited, and re-activated).
--          The wizard uses sp_waitlist_request_types_get (active only); this is the admin variant.
--          Order is by id, which matches the seeded/display order (no explicit sort_order column yet).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_get_all;

CREATE PROCEDURE sp_waitlist_request_types_get_all()
SELECT
    id,
    public_id,
    request_type,
    control,
    flow,
    requires_text_input,
    prompt_text,
    min_length,
    max_length,
    default_image_path,
    category,
    item_id,
    center_data_grid_fields_json,
    is_active,
    created_utc,
    updated_utc
FROM waitlist_request_types
ORDER BY id ASC;
