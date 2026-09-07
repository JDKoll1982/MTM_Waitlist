-- Create procedure: sp_waitlist_request_types_delete
-- Engine: MySQL 5.7
-- Purpose: Delete a real (non-mock) request type and its subtypes by type id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_types_delete;

CREATE PROCEDURE sp_waitlist_request_types_delete(IN p_id BIGINT)
DELETE waitlist_request_subtypes, waitlist_request_types
FROM waitlist_request_types
LEFT JOIN waitlist_request_subtypes ON waitlist_request_subtypes.request_type_id = waitlist_request_types.id
WHERE waitlist_request_types.id = p_id;
