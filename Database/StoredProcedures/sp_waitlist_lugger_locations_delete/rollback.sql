-- Rollback: sp_waitlist_lugger_locations_delete
-- Engine: MySQL 5.7
-- Reversal of 013-lugger-location-config (task T015)
--
-- Drops the lugger removal. It owns no data of its own: the rows it writes live in
-- waitlist_lugger_arrangement_changes, which predates this procedure, and it deletes rows from
-- waitlist_lugger_locations, which the table's own rollback removes.
--
-- Dropping rather than leaving it is the honest reversal. A leftover procedure would be a delete path with no
-- caller and no owner, and the first thing it would do is take a lugger and its assignments out of an
-- arrangement nobody has approved the shape of yet.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_delete;
