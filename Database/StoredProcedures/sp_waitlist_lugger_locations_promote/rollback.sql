-- Rollback: sp_waitlist_lugger_locations_promote
-- Engine: MySQL 5.7
-- Reversal of 013-lugger-location-config (task T017)
--
-- Drops the explicit promotion. It owns no data of its own: the rows it writes live in
-- waitlist_lugger_locations, waitlist_lugger_scrap_assignments and waitlist_lugger_arrangement_changes, which
-- all predate it, and the dump points already moved through it stay where they are.
--
-- A leftover procedure would be a write path with no caller and no reader, and it is the one path that moves a
-- dump point without going through the status transitions, so it is dropped rather than left behind.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_promote;
