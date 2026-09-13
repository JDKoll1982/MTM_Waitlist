-- Create procedure: sp_waitlist_request_item_observed_average_get
-- Engine: MySQL 5.7
-- Purpose: Per Item, how long its completed requests actually took. This is the observed half of the pair a
--          screen shows beside the configured allotment, and it is derived and never stored (FR-019).
-- Contract: specs/004-unified-card-item-picker/contracts/item-configuration.md section 4.
--   item                    the Item code.
--   completed_request_count  how many of that Item's requests completed.
--   average_seconds         the mean of completed_utc - accepted_utc over those requests.
-- The rules, and why each is written the way it is:
--   * Completed requests only. The stored status vocabulary is unchanged (FR-032), so this filters on the
--     existing 'Completed' value and adds no status.
--   * A request missing either endpoint is excluded; the average never treats a missing timestamp as zero.
--   * released_utc is deliberately NOT read. A request that was released and later completed is one row with
--     one accepted -> completed pair, so it contributes exactly once. Reading released_utc as a second
--     interval is how such a request would be counted twice, which is why it is absent here.
--   * No time window. The average covers all of an Item's completed requests.
--   * An Item with no completed request yields no row, so the screen shows the configured minutes alone
--     rather than a fabricated zero (FR-026).
-- Read-only: this procedure writes nothing, and the observed value never reaches
-- sp_waitlist_request_item_allotted_minutes_update (FR-018).

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_request_item_observed_average_get;

CREATE PROCEDURE sp_waitlist_request_item_observed_average_get()
SELECT
    q.item,
    COUNT(*) AS completed_request_count,
    AVG(TIMESTAMPDIFF(SECOND, q.accepted_utc, q.completed_utc)) AS average_seconds
FROM waitlist_requests_queue q
WHERE q.status = 'Completed'
  AND q.accepted_utc IS NOT NULL
  AND q.completed_utc IS NOT NULL
GROUP BY q.item
ORDER BY q.item ASC;
