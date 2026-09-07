-- Create procedure: sp_waitlist_request_subtypes_delete
-- Engine: MySQL 5.7
-- Purpose: Delete a real (non-mock) request subtype by id.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_subtypes_delete;

CREATE PROCEDURE sp_waitlist_request_subtypes_delete(IN p_id BIGINT)
DELETE FROM waitlist_request_subtypes WHERE id = p_id;
