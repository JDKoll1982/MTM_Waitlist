-- Rollback: sp_waitlist_lugger_arrangement_list
-- Engine: MySQL 5.7
-- Reversal of 013-lugger-location-config (task T021)
--
-- Drops the arrangement read. It owns no data at all: it is one SELECT over waitlist_lugger_locations and
-- waitlist_lugger_scrap_assignments, which both predate it.
--
-- A leftover procedure would be a read path with no caller, and it would keep answering with the ten-column
-- shape after the feature that defined those columns is gone.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_arrangement_list;
