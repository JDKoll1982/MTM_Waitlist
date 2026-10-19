-- Rollback: sp_waitlist_lugger_locations_insert
-- Engine: MySQL 5.7
-- Reversal of 013-lugger-location-config (task T013)
--
-- Drops the add. It owns no data of its own: the rows it writes live in waitlist_lugger_locations and
-- waitlist_lugger_arrangement_changes, which both predate it, and the luggers already added through it stay.
--
-- Dropping rather than leaving it is the honest reversal: a leftover procedure would be a write path with no
-- caller and no reader, and the first thing it could do is add a lugger nothing in the application can name.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_insert;
