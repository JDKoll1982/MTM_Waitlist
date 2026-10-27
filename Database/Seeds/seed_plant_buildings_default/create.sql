-- Seed: seed_plant_buildings_default
-- Engine: MySQL 5.7
--
-- Purpose: the plant's two buildings, and the default answer for every computer that has not been set up yet.
--
-- What the two rows are
--   `Expo Drive` and `Vits Drive` are the two values `setup_work_centers_catalog.building` already carries, so
--   the catalog names exactly what the work centres record and no work centre is left standing in a building the
--   choosers cannot offer. Expo Drive ranks first because it is the plant's first building, which is why it is
--   also the default a computer without an answer falls back to.
--
-- The default computer answer
--   `core_computer_buildings.building_name` defaults to Expo Drive for the same reason, and this seed writes that
--   answer for every computer already in the registry so a machine that was set up before this table existed
--   opens on Expo Drive instead of on no answer at all. Machine setup replaces the row with the building chosen
--   there, and re-running this seed does not overwrite a chosen answer because only computers with no row are
--   touched. A freshly built store has no computers, so nothing is assigned there: the machine that runs setup
--   registers itself and records its own building at the same time.
--
-- Re-running
--   The catalog insert is `ON DUPLICATE KEY UPDATE` against `building_name`, so a re-run keeps the two names
--   exactly once. The assignment insert is filtered by `NOT EXISTS`, so a second run has nothing to write.

USE mtm_waitlist;

SET NAMES utf8mb4;

INSERT INTO
    core_buildings_catalog (
        public_id,
        building_name,
        is_active,
        sort_rank,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'Expo Drive',
        1,
        10,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Vits Drive',
        1,
        20,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    is_active = VALUES(is_active),
    sort_rank = VALUES(sort_rank),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    core_computer_buildings (
        public_id,
        computer_id,
        building_name,
        created_utc,
        updated_utc
    )
SELECT UUID(),
       r.id,
       'Expo Drive',
       UTC_TIMESTAMP(),
       UTC_TIMESTAMP()
FROM core_computers_registry r
WHERE NOT EXISTS (
        SELECT 1
        FROM core_computer_buildings cb
        WHERE cb.computer_id = r.id
    );
