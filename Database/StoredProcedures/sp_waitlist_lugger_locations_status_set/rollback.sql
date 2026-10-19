-- Rollback: sp_waitlist_lugger_locations_status_set
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T016)
--
-- Drops the transition engine. It owns no data: the status it writes lives in waitlist_lugger_locations and the rows
-- it appends live in waitlist_lugger_arrangement_changes, both of which predate this feature and reverse with their
-- own rollback.
--
-- Dropping rather than leaving it is the honest reversal: before this feature there was no lugger set and no status
-- to move, so a leftover procedure would be a write path with no caller, no owner and no reader, and the first thing
-- it would do is move a dump point under an arrangement nobody is checking.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_status_set;
