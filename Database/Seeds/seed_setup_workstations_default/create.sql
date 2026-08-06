-- Seed: seed_setup_workstations_default
-- Engine: MySQL 5.7

USE mtm_waitlist;

INSERT IGNORE INTO
    setup_workstations_catalog (
        public_id,
        workstation_name,
        is_active,
        sort_rank,
        created_by_user_id,
        updated_by_user_id,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'Press 01A',
        1,
        10,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Press 02B',
        1,
        20,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Press 03C',
        1,
        30,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Press 04D',
        1,
        40,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Laser Cell 01',
        1,
        50,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Laser Cell 02',
        1,
        60,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Brake Press A',
        1,
        70,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Brake Press B',
        1,
        80,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Weld Bay 01',
        1,
        90,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Weld Bay 02',
        1,
        100,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Assembly Line North',
        1,
        110,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Assembly Line South',
        1,
        120,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Packing Station Alpha',
        1,
        130,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Packing Station Bravo',
        1,
        140,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'Inspection Cell Delta',
        1,
        150,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    );