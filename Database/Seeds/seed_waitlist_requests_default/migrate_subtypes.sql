-- Migration: normalize_waitlist_queue_subtypes
-- Engine: MySQL 5.7 / 8.x
-- Database: mtm_waitlist
--
-- Purpose: Bring already-persisted waitlist_requests_queue rows in line with the real
--          request-type/subtype catalog so that WaitlistRequestTitles.For(...) resolves a
--          friendly card title instead of the "Type / Subtype" fallback.
--
--          The Waitlist UI computes card titles keyed on the CATALOG subtype names (e.g.
--          "Pickup WIP", "Pickup FG", "Pickup NCM", "Wrong Coil @ press", Coil subtype
--          "Pickup"). Rows seeded before the catalog carried legacy subtype strings
--          ("WIP", "Finished Goods", "NCM", "Wrong Coil", Coil "Pickup Coil", Scrap
--          "Scrap", Forklift Assist repeated as its own subtype). Those legacy values
--          match no catalog key and render as a fallback. This migration rewrites only
--          those legacy values; rows that already use catalog names are untouched.
--
-- Scope: waitlist_requests_queue only. The audit table stores no subtype. The real
--        catalog tables (waitlist_request_types / waitlist_request_subtypes) and the
--        mock catalog are NOT modified.
--
-- Idempotent / safe to run: every UPDATE targets a (request_type, subtype) legacy pair,
-- so re-running finds no further legacy rows and changes nothing.

USE mtm_waitlist;

-- 1) Coil requests: legacy "Pickup Coil" action -> catalog Coil subtype "Pickup".
--    (Catalog Coil subtypes are actions: Bring / Pickup / Wrong Coil @ press / ...)
UPDATE waitlist_requests_queue
SET subtype = 'Pickup'
WHERE request_type = 'Coil'
  AND subtype = 'Pickup Coil';

-- 2) Coil "Wrong Coil" report -> catalog Coil subtype "Wrong Coil @ press".
UPDATE waitlist_requests_queue
SET subtype = 'Wrong Coil @ press'
WHERE request_type = 'Coil'
  AND subtype = 'Wrong Coil';

-- 3) Pickup legacy subtype abbreviations -> full catalog Pickup subtype names.
UPDATE waitlist_requests_queue SET subtype = 'Pickup WIP' WHERE request_type = 'Pickup' AND subtype = 'WIP';
UPDATE waitlist_requests_queue SET subtype = 'Pickup FG'  WHERE request_type = 'Pickup' AND subtype = 'Finished Goods';
UPDATE waitlist_requests_queue SET subtype = 'Pickup NCM' WHERE request_type = 'Pickup' AND subtype = 'NCM';
-- NOTE: Pickup "Outside Service" is intentionally left as-is; it is a real O/S request kind
--       and has a dedicated map entry (PICKUP\0OUTSIDE SERVICE).

-- 4) Scrap legacy "Scrap" (no catalog Scrap subtype) -> catalog Scrap subtype "Empty".
UPDATE waitlist_requests_queue
SET subtype = 'Empty'
WHERE request_type = 'Scrap'
  AND subtype = 'Scrap';

-- 5) Forklift Assist has NO catalog subtypes; clear the repeated subtype so the title
--    resolves to the request type alone ("Forklift Assist") instead of "Forklift Assist / Forklift Assist".
UPDATE waitlist_requests_queue
SET subtype = NULL
WHERE request_type = 'Forklift Assist'
  AND subtype = 'Forklift Assist';

-- Optional verification (expected count after migration):
-- SELECT request_type, subtype, COUNT(*) FROM waitlist_requests_queue GROUP BY request_type, subtype ORDER BY request_type, subtype;
