-- Create procedure: sp_waitlist_request_item_allotted_minutes_update
-- Engine: MySQL 5.7
-- Purpose: Write the configured allotment for one Item. This is the ONLY writer of a configured allotment:
--          the observed average is display data, and it is never written back as though it were configured
--          (FR-018, FR-019).
-- Contract: specs/004-unified-card-item-picker/contracts/item-configuration.md section 3.
--   p_item            the Item code whose allotment is being set.
--   p_allotted_minutes the configured minutes. A non-NULL value is clamped here to 1..1440 as well as in the
--                     caller (UrgencySettingsService.SetMaxAllottedAsync clamps to the same range). A NULL
--                     value clears the configured figure, so the Item falls back to the labelled 15-minute
--                     default rather than to a stale number.
--   p_updated_by_user_id the auditing column this table's row carries, matching the repo's other update
--                     procedures.
-- Returns the affected-row count through the caller's non-query seam, so a caller can tell an Item that does
-- not exist (0 rows) from one that does.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_item_allotted_minutes_update;

CREATE PROCEDURE sp_waitlist_request_item_allotted_minutes_update(
    IN p_item VARCHAR(64),
    IN p_allotted_minutes INT,
    IN p_updated_by_user_id BIGINT
)
UPDATE waitlist_request_item_configs
SET allotted_minutes = CASE
        WHEN p_allotted_minutes IS NULL THEN NULL
        ELSE LEAST(GREATEST(p_allotted_minutes, 1), 1440)
    END,
    updated_by_user_id = p_updated_by_user_id,
    updated_utc = UTC_TIMESTAMP()
WHERE item = TRIM(COALESCE(p_item, ''));
