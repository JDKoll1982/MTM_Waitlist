-- Rollback: sp_waitlist_lugger_scrap_assignments_set
-- Engine: MySQL 5.7
-- Reversal of 013-lugger-location-config (task T019)
--
-- Drops the assignment write. It owns no data of its own: the rows it writes live in
-- waitlist_lugger_scrap_assignments and waitlist_lugger_arrangement_changes, which both predate this procedure, and
-- the arrangement the shipped seed writes is left exactly as it is.
--
-- Dropping rather than leaving it is the honest reversal. A leftover procedure would be a write path with no caller
-- and no owner, and the first thing it would do is renumber a scrap type's try order and hand out a dump point that
-- nothing has approved the shape of yet.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_scrap_assignments_set;
