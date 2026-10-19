-- Stored Procedure: sp_waitlist_lugger_locations_list
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T012)
--
-- Purpose: read every named lugger the Material Handler Lead maintains, in the order the set is displayed, so the
--          settings page, the summary and the notice all draw the same list from one call (FR-001).
--
-- Contract:
--   IN  none
--
--   OUT id, public_id, lugger_name, status, status_reason, display_position, created_utc, updated_utc
--       one row per lugger that exists, ordered by display_position ascending then lugger_name ascending.
--
-- Guarantee:
--   A removed lugger has no row, because removal deletes it (research D3). status is always one of the four pinned
--   strings, and status_reason is null except where the lugger is Do Not Use. display_position is what the caller
--   orders by; the second key is only a tie-break, so two luggers written at one position still come back in a
--   stable order.
--
--   This is a read. It opens no transaction, writes nothing and appends no change row.
-- ============================================================

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_list;

CREATE PROCEDURE sp_waitlist_lugger_locations_list()
SELECT
    id,
    public_id,
    lugger_name,
    status,
    status_reason,
    display_position,
    created_utc,
    updated_utc
FROM waitlist_lugger_locations
ORDER BY
    display_position ASC,
    lugger_name ASC;
