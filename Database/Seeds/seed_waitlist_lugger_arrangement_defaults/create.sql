-- Seed: seed_waitlist_lugger_arrangement_defaults
-- Engine: MySQL 5.7
-- Feature: 013-lugger-location-config (task T005)
-- Purpose: a fresh installation ships the seven named luggers at display_position 1 to 7 and the seven
--          assignments that send each scrap type to the lugger it dumps through, so no window exists in which
--          the waitlist draws a scrap card without a location (FR-002, FR-008, FR-013).
-- Source: specs/013-lugger-location-config/contracts/persistence-and-permission.md section 3 pins the names,
--         the statuses and the pairings; data-model.md sections 1 and 2 define the columns.
--
-- Idempotent, never destructive
--   The whole row set is written only when waitlist_lugger_locations is empty, and every statement lands in one
--   transaction, so a second run adds nothing and a store that already holds luggers is left alone. Nothing is
--   truncated and nothing is overwritten: a lugger the lead has renamed keeps the name the lead gave it, which
--   is what lets this file reach an installed store without a reinstall (research D4).
--
-- Where the dump point sits
--   Each primary pairing is inserted with is_dump_point = 1 at sort_position 1, and `Extra Lugger (Outside)` is
--   seeded as the second choice for `3003 Aluminum` with is_dump_point = 0 at sort_position 2. The role lives on
--   the pairing, not on the lugger, so a status change later moves the flag without touching this seed
--   (FR-013, research D2).
--
-- Scrap type keys
--   scrap_type_key is LOWER(TRIM(scrap_type)), the value every lookup matches on (FR-009, research D5). The key
--   is derived in the insert rather than written as a literal, so the two columns can never disagree. The setup
--   workflow spells one type `5052 aluminum`; the assignment is pinned here as `5052 Aluminum`, and the two
--   resolve to the same row because matching ignores letter case.
--
-- The reversal
--   rollback.sql deletes only these seven luggers, by name. Their assignments leave with them through
--   fk_waitlist_lugger_assignments_lugger_locations_lugger_id (ON DELETE CASCADE).

USE mtm_waitlist;

SET NAMES utf8mb4;

START TRANSACTION;

SET @seed_lugger_arrangement_allowed = (
    SELECT COUNT(*) = 0
    FROM waitlist_lugger_locations
);

INSERT INTO
    waitlist_lugger_locations (
        public_id,
        lugger_name,
        status,
        status_reason,
        display_position,
        created_utc,
        updated_utc
    )
SELECT
    UUID(),
    d.lugger_name,
    d.status,
    NULL,
    d.display_position,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
FROM (
    SELECT
        1 AS display_position,
        'Door 6 Lugger' AS lugger_name,
        'Available' AS status
    UNION ALL
    SELECT 2, 'Door 7 Lugger', 'Available'
    UNION ALL
    SELECT 3, 'Door 8 Lugger', 'Available'
    UNION ALL
    SELECT 4, 'Door 9 Lugger', 'Available'
    UNION ALL
    SELECT 5, 'Extra Lugger (Outside)', 'Backup'
    UNION ALL
    SELECT 6, 'Skeleton Lugger', 'Available'
    UNION ALL
    SELECT 7, 'Scrap Trailer', 'Available'
) AS d
WHERE @seed_lugger_arrangement_allowed = 1;

INSERT INTO
    waitlist_lugger_scrap_assignments (
        public_id,
        lugger_id,
        scrap_type,
        scrap_type_key,
        is_dump_point,
        sort_position,
        created_utc,
        updated_utc
    )
SELECT
    UUID(),
    l.id,
    d.scrap_type,
    LOWER(TRIM(d.scrap_type)),
    d.is_dump_point,
    d.sort_position,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP()
FROM (
    SELECT
        '3003 Aluminum' AS scrap_type,
        'Door 6 Lugger' AS lugger_name,
        1 AS is_dump_point,
        1 AS sort_position
    UNION ALL
    SELECT '3003 Aluminum', 'Extra Lugger (Outside)', 0, 2
    UNION ALL
    SELECT 'Galvanized Steel', 'Door 7 Lugger', 1, 1
    UNION ALL
    SELECT '5052 Aluminum', 'Door 8 Lugger', 1, 1
    UNION ALL
    SELECT 'Steel', 'Door 9 Lugger', 1, 1
    UNION ALL
    SELECT 'Skeleton', 'Skeleton Lugger', 1, 1
    UNION ALL
    SELECT 'Gaylord', 'Scrap Trailer', 1, 1
) AS d
INNER JOIN waitlist_lugger_locations AS l ON l.lugger_name = d.lugger_name
WHERE @seed_lugger_arrangement_allowed = 1;

COMMIT;
