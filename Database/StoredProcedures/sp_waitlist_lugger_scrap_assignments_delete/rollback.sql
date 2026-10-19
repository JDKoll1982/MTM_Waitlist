-- Rollback: sp_waitlist_lugger_scrap_assignments_delete
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T020)
--
-- Drops the unassignment write. It owns no data: the pairing it removes lives in
-- waitlist_lugger_scrap_assignments and the rows it appends live in waitlist_lugger_arrangement_changes, both of
-- which predate this feature and reverse with their own rollback.
--
-- Dropping rather than leaving it is the honest reversal: before this feature there was no scrap type assignment at
-- all, so a leftover procedure would be a removal path with no caller, no owner and no reader.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_scrap_assignments_delete;
