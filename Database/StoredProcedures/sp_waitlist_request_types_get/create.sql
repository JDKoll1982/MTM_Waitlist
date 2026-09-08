-- Create procedure: sp_waitlist_request_types_get
-- Engine: MySQL 5.7
-- Purpose: Return the active real (non-mock) request-type catalog rows for the New-Request wizard.
--          Source of truth for request types (was Assets/Config/waitlist-request-types.json).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_get;

CREATE PROCEDURE sp_waitlist_request_types_get()
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
    is_active
FROM waitlist_request_types
WHERE is_active = 1
ORDER BY id ASC;
