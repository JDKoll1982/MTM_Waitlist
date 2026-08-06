-- Rollback seed: seed_setup_workstations_default

USE mtm_waitlist;

DELETE FROM setup_workstations_catalog
WHERE
    workstation_name IN (
        'Press 01A',
        'Press 02B',
        'Press 03C',
        'Press 04D',
        'Laser Cell 01',
        'Laser Cell 02',
        'Brake Press A',
        'Brake Press B',
        'Weld Bay 01',
        'Weld Bay 02',
        'Assembly Line North',
        'Assembly Line South',
        'Packing Station Alpha',
        'Packing Station Bravo',
        'Inspection Cell Delta'
    );