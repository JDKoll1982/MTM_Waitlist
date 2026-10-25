-- Rollback seed: seed_waitlist_lugger_arrangement_defaults
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T005)
-- Purpose: remove the seven luggers this seed writes, by name, so the store returns to the state it held before
--          the seed ran.
-- Scope: only the seven pinned names. A lugger the lead added is not one of them and is left in place.
-- Cascade: fk_waitlist_lugger_assignments_lugger_locations_lugger_id takes each removed lugger's assignments
--          with it (ON DELETE CASCADE), so no assignment is left pointing at a lugger that no longer exists.
-- Transaction: one transaction, so a partial reversal cannot leave a lugger without its pairings or the
--              reverse.

USE mtm_waitlist;

SET NAMES utf8mb4;

START TRANSACTION;

DELETE FROM waitlist_lugger_locations
WHERE
    lugger_name IN (
        'Door 6 Lugger',
        'Door 7 Lugger',
        'Door 8 Lugger',
        'Door 9 Lugger',
        'Extra Lugger (Outside)',
        'Skeleton Lugger',
        'Scrap Trailer'
    );

COMMIT;
