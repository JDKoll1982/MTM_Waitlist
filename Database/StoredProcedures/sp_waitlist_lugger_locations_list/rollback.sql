-- Rollback: sp_waitlist_lugger_locations_list
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T012)
--
-- Drops the lugger list read. It owns no data: the rows it returns live in waitlist_lugger_locations, which the
-- table's own rollback reverses.
--
-- Dropping rather than leaving it is the honest reversal. Before this feature there was no lugger set at all, so a
-- leftover procedure would be a reader with no writer, no caller and no owner.

USE mtm_waitlist;

DROP PROCEDURE IF EXISTS sp_waitlist_lugger_locations_list;
