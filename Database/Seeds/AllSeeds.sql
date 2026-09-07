-- Dev/test masked baseline seed
-- Do not use real production identity/session data in non-production.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE auth_roles_catalog;

INSERT INTO
    auth_roles_catalog (
        public_id,
        role_code,
        role_name,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'material_handler',
        'Material Handler',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'production',
        'Production',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'production_lead',
        'Production Lead',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'setup',
        'Setup',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'setup_lead',
        'Setup Lead',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'plant_manager',
        'Plant Manager',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'developer',
        'Developer',
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    role_name = VALUES(role_name),
    updated_utc = VALUES(updated_utc);

TRUNCATE TABLE config_settings_values;

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_int,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES (
        UUID(),
        'sessions.retention_inactive_days',
        'all_users',
        'all_users',
        30,
        'int',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'waitlist.resolved_retention_days',
        'all_users',
        'all_users',
        90,
        'int',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'settings.history_retention_days',
        'all_users',
        'all_users',
        30,
        'int',
        NULL,
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    scope_type = VALUES(scope_type),
    scope_key = VALUES(scope_key),
    setting_value_int = VALUES(setting_value_int),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

TRUNCATE TABLE core_users_profiles;

INSERT INTO
    core_users_profiles (
        public_id,
        username_normalized,
        password_hash,
        password_salt,
        require_password_change,
        display_name,
        employee_identifier,
        is_active,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'johnk',
        '0000',
        NULL,
        1,
        'John Koll',
        '6229',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'jkoll',
        '0000',
        NULL,
        1,
        'John Koll',
        '6229',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    password_hash = VALUES(password_hash),
    password_salt = VALUES(password_salt),
    require_password_change = VALUES(require_password_change),
    display_name = VALUES(display_name),
    employee_identifier = VALUES(employee_identifier),
    updated_utc = VALUES(updated_utc);

TRUNCATE TABLE auth_roles_assignments;

INSERT INTO
    auth_roles_assignments (
        public_id,
        user_id,
        role_id,
        assigned_utc,
        assigned_by_user_id
    )
SELECT UUID(), users.id, roles.id, UTC_TIMESTAMP(), users.id
FROM
    core_users_profiles users
    INNER JOIN auth_roles_catalog roles ON roles.role_code = 'developer'
WHERE
    users.username_normalized IN ('johnk', 'jkoll')
ON DUPLICATE KEY UPDATE
    assigned_utc = VALUES(assigned_utc),
    assigned_by_user_id = VALUES(assigned_by_user_id);

TRUNCATE TABLE core_buildings_catalog;

INSERT INTO
    core_buildings_catalog (
        public_id,
        building_code,
        building_name,
        is_active,
        created_utc,
        updated_utc,
        updated_by_user_id
    )
VALUES (
        UUID(),
        'expo_drive',
        'Expo Drive',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP(),
        NULL
    ),
    (
        UUID(),
        'vits_drive',
        'Vits Drive',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP(),
        NULL
    )
ON DUPLICATE KEY UPDATE
    building_name = VALUES(building_name),
    is_active = VALUES(is_active),
    updated_utc = VALUES(updated_utc);

TRUNCATE TABLE core_computers_registry;

INSERT INTO
    core_computers_registry (
        public_id,
        computer_name,
        hostname_normalized,
        mac_address_normalized,
        display_name,
        description,
        is_registered,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'johnspc',
        'johnspc',
        'd8-43-ae-47-d0-d6',
        'John''s Computer',
        NULL,
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'mtmfg-161',
        'mtmfg-161',
        'f4-f1-9e-38-64-d3',
        'MTMFG 161',
        NULL,
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    computer_name = VALUES(computer_name),
    updated_utc = VALUES(updated_utc);
    
-- Seed: seed_setup_work_centers_default
-- Engine: MySQL 5.7

USE mtm_waitlist;

TRUNCATE TABLE setup_work_centers_catalog;

INSERT IGNORE INTO
    setup_work_centers_catalog (
        public_id,
        work_center_name,
        building,
        is_active,
        sort_rank,
        created_by_user_id,
        updated_by_user_id,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        '100-3',
        'Expo Drive',
        1,
        10,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-6',
        'Expo Drive',
        1,
        20,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-7',
        'Expo Drive',
        1,
        30,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-8',
        'Expo Drive',
        1,
        40,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-9',
        'Expo Drive',
        1,
        50,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-12',
        'Expo Drive',
        1,
        60,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-13',
        'Expo Drive',
        1,
        70,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-14',
        'Expo Drive',
        1,
        80,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-15',
        'Expo Drive',
        1,
        90,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-16',
        'Expo Drive',
        1,
        100,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-17',
        'Expo Drive',
        1,
        110,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-18',
        'Expo Drive',
        1,
        120,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-19',
        'Expo Drive',
        1,
        130,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-20',
        'Expo Drive',
        1,
        140,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-21',
        'Expo Drive',
        1,
        150,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-22',
        'Expo Drive',
        1,
        160,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-23',
        'Expo Drive',
        1,
        170,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-24',
        'Expo Drive',
        1,
        180,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-25',
        'Expo Drive',
        1,
        190,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-28',
        'Expo Drive',
        1,
        200,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-1807',
        'Expo Drive',
        1,
        210,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        '100-1806',
        'Expo Drive',
        1,
        220,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'V100-33',
        'Vits Drive',
        1,
        230,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'V100-34',
        'Vits Drive',
        1,
        240,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'V100-35',
        'Vits Drive',
        1,
        250,
        NULL,
        NULL,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    );

SET FOREIGN_KEY_CHECKS = 1;

-- Seed: seed_waitlist_requests_default
-- Engine: MySQL 5.7
-- Source: demo dataset for the Waitlist work delivered in PromptFiles 01-06:
--   01 lifecycle (Pending/Accepted/Completed/Canceled + requested/accepted/completed/canceled + note)
--   02 coil requests (Pickup Coil, Wrong Coil) and part/type variety for the coil card
--   03 list/detail across request types/subtypes (Coil, Pickup WIP/FG/NCM/Outside, Scrap, Flatstock, Forklift, Other)
--   04 requester cancel-own + "My Requests" (rows from the signed-in requester 6229 AND other requester 5000)
--   05/06 ignored-locations + location grid are NOT persisted in this MySQL table (mock/Infor Visual), so they
--       are exercised by the coil/request rows above plus app-side sample data.
-- Idempotent: TRUNCATE both tables first (this is the seed's own demo set), then insert deterministic rows.
-- Consumed by sp_waitlist_request_list / _get / _status_update / _audit_insert and the Waitlist UI.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE waitlist_requests_audit;
TRUNCATE TABLE waitlist_requests_queue;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_requests_queue (
    public_id, building, work_center, request_type, subtype, input_value, active_setup_job_id,
    work_center_name, requester_employee_number, requester_employee_name, status,
    requested_utc, target_time_utc, is_overdue, assigned_material_handler,
    cancellation_reason, canceled_utc, canceled_by_employee_number,
    note, accepted_utc, completed_utc, released_utc, created_utc, updated_utc
)
VALUES
    -- Pending (open) -> returned by sp_waitlist_request_list; drives list/card + wait time + status badge
    ('f0000000-0001-4000-8000-000000000001','Expo Drive','100-3','Coil','Pickup',NULL,'100-3','100-3','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 35 MINUTE, (UTC_TIMESTAMP() - INTERVAL 35 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        'Coil pickup for press 100-3.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 35 MINUTE, UTC_TIMESTAMP() - INTERVAL 35 MINUTE),
    ('f0000000-0002-4000-8000-000000000002','Expo Drive','100-3','Coil','Wrong Coil @ press','Wrong material at press - expected MMC0001000.','100-3','100-3','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 12 MINUTE, (UTC_TIMESTAMP() - INTERVAL 12 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        'Reports the staged coil is the wrong one.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 12 MINUTE, UTC_TIMESTAMP() - INTERVAL 12 MINUTE),
    ('f0000000-0003-4000-8000-000000000003','Expo Drive','100-6','Pickup','Pickup WIP',NULL,'100-6','100-6','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 50 MINUTE, (UTC_TIMESTAMP() - INTERVAL 50 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 50 MINUTE, UTC_TIMESTAMP() - INTERVAL 50 MINUTE),
    ('f0000000-0004-4000-8000-000000000004','Expo Drive','100-5','Pickup','Pickup FG',NULL,'100-5','100-5','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 22 MINUTE, (UTC_TIMESTAMP() - INTERVAL 22 MINUTE) + INTERVAL 45 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 22 MINUTE, UTC_TIMESTAMP() - INTERVAL 22 MINUTE),
    ('f0000000-0005-4000-8000-000000000005','Expo Drive','100-11','Pickup','Pickup NCM',NULL,'100-11','100-11','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 18 MINUTE, (UTC_TIMESTAMP() - INTERVAL 18 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 18 MINUTE, UTC_TIMESTAMP() - INTERVAL 18 MINUTE),
    ('f0000000-0006-4000-8000-000000000006','Expo Drive','100-12','Pickup','Outside Service',NULL,'100-12','100-12','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 9 MINUTE, (UTC_TIMESTAMP() - INTERVAL 9 MINUTE) + INTERVAL 60 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 9 MINUTE, UTC_TIMESTAMP() - INTERVAL 9 MINUTE),
    -- Other requester (5000) open requests -> excluded from signed-in user's "My Requests"; cancel-own by 6229 must be denied
    ('f0000000-0007-4000-8000-000000000007','Expo Drive','100-14','Pickup','Pickup WIP',NULL,'100-14','100-14','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 30 MINUTE, (UTC_TIMESTAMP() - INTERVAL 30 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 30 MINUTE, UTC_TIMESTAMP() - INTERVAL 30 MINUTE),
    ('f0000000-0008-4000-8000-000000000008','Expo Drive','100-15','Other','General Text Entry','Need a line supervisor for a quick walkthrough.','100-15','100-15','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 14 MINUTE, (UTC_TIMESTAMP() - INTERVAL 14 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 14 MINUTE, UTC_TIMESTAMP() - INTERVAL 14 MINUTE),
    ('f0000000-0009-4000-8000-000000000009','Vits Drive','V100-40','Forklift Assist',NULL,'Need a forklift to move a pallet to staging.','V100-40','V100-40','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 26 MINUTE, (UTC_TIMESTAMP() - INTERVAL 26 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 26 MINUTE, UTC_TIMESTAMP() - INTERVAL 26 MINUTE),
    -- Accepted (In Progress) -> returned by list; claimed by a handler; lifecycle accepted_utc
    ('f0000000-0010-4000-8000-000000000010','Expo Drive','100-7','Coil','Pickup',NULL,'100-7','100-7','6229','John Koll','Accepted',
        UTC_TIMESTAMP() - INTERVAL 80 MINUTE, (UTC_TIMESTAMP() - INTERVAL 80 MINUTE) + INTERVAL 30 MINUTE, 1, '6229', NULL, NULL, NULL,
        'Overdue coil move in progress.', UTC_TIMESTAMP() - INTERVAL 75 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 80 MINUTE, UTC_TIMESTAMP() - INTERVAL 75 MINUTE),
    ('f0000000-0011-4000-8000-000000000011','Expo Drive','100-8','Other','General Text Entry','Changeover review requested.','100-8','100-8','6229','John Koll','Accepted',
        UTC_TIMESTAMP() - INTERVAL 25 MINUTE, (UTC_TIMESTAMP() - INTERVAL 25 MINUTE) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 20 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 25 MINUTE, UTC_TIMESTAMP() - INTERVAL 20 MINUTE),
    ('f0000000-0012-4000-8000-000000000012','Expo Drive','100-16','Pickup','Pickup NCM',NULL,'100-16','100-16','5000','Other User','Accepted',
        UTC_TIMESTAMP() - INTERVAL 40 MINUTE, (UTC_TIMESTAMP() - INTERVAL 40 MINUTE) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 33 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 40 MINUTE, UTC_TIMESTAMP() - INTERVAL 33 MINUTE),
    -- Completed (Done) -> resolved, retained; accepted_utc + completed_utc
    ('f0000000-0013-4000-8000-000000000013','Expo Drive','100-9','Scrap','Empty','Scrapped punch slugs at press 100-9.','100-9','100-9','6229','John Koll','Completed',
        UTC_TIMESTAMP() - INTERVAL 3 HOUR, (UTC_TIMESTAMP() - INTERVAL 3 HOUR) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        'Lugger filled and confirmed.', UTC_TIMESTAMP() - INTERVAL 170 MINUTE, UTC_TIMESTAMP() - INTERVAL 120 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 3 HOUR, UTC_TIMESTAMP() - INTERVAL 120 MINUTE),
    ('f0000000-0014-4000-8000-000000000014','Vits Drive','V100-41','Pickup','Pickup FG',NULL,'V100-41','V100-41','5000','Other User','Completed',
        UTC_TIMESTAMP() - INTERVAL 4 HOUR, (UTC_TIMESTAMP() - INTERVAL 4 HOUR) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 230 MINUTE, UTC_TIMESTAMP() - INTERVAL 150 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 4 HOUR, UTC_TIMESTAMP() - INTERVAL 150 MINUTE),
    -- Canceled -> cancel-own path (file 04): creator cancels own Waiting request; canceled_by = creator
    ('f0000000-0015-4000-8000-000000000015','Expo Drive','100-19','Pickup','Outside Service',NULL,'100-19','100-19','6229','John Koll','Canceled',
        UTC_TIMESTAMP() - INTERVAL 2 HOUR, (UTC_TIMESTAMP() - INTERVAL 2 HOUR) + INTERVAL 30 MINUTE, 0, NULL,
        'Operator no longer needs this pickup.', UTC_TIMESTAMP() - INTERVAL 110 MINUTE, '6229',
        'Canceled by requester before handling.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 2 HOUR, UTC_TIMESTAMP() - INTERVAL 110 MINUTE),
    ('f0000000-0016-4000-8000-000000000016','Expo Drive','100-20','Coil','Pickup',NULL,'100-20','100-20','5000','Other User','Canceled',
        UTC_TIMESTAMP() - INTERVAL 5 HOUR, (UTC_TIMESTAMP() - INTERVAL 5 HOUR) + INTERVAL 30 MINUTE, 0, NULL,
        'Duplicate of an earlier request.', UTC_TIMESTAMP() - INTERVAL 280 MINUTE, '5000',
        'Other requester canceled own request.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 5 HOUR, UTC_TIMESTAMP() - INTERVAL 280 MINUTE);

-- Audit rows so the Waitlist audit trail (file 01/03) and future analytics have transitions to read.
INSERT INTO waitlist_requests_audit (
    public_id, request_public_id, from_status, to_status, event_type,
    actor_employee_number, actor_employee_name, details, occurred_utc
)
VALUES
    ('a0000000-0001-4000-8000-000000000001','f0000000-0001-4000-8000-000000000001',NULL,NULL,'Created','6229','John Koll','Request submitted for press 100-3.',UTC_TIMESTAMP() - INTERVAL 35 MINUTE),
    ('a0000000-0002-4000-8000-000000000002','f0000000-0002-4000-8000-000000000002',NULL,NULL,'Created','6229','John Koll','Wrong coil reported.',UTC_TIMESTAMP() - INTERVAL 12 MINUTE),
    ('a0000000-0003-4000-8000-000000000003','f0000000-0007-4000-8000-000000000007',NULL,NULL,'Created','5000','Other User','Other requester WIP request.',UTC_TIMESTAMP() - INTERVAL 30 MINUTE),
    ('a0000000-0010-4000-8000-000000000010','f0000000-0010-4000-8000-000000000010',NULL,'Pending','Created','6229','John Koll','Request created.',UTC_TIMESTAMP() - INTERVAL 80 MINUTE),
    ('a0000000-0011-4000-8000-000000000011','f0000000-0010-4000-8000-000000000010','Pending','Accepted','Accepted','6229','John Koll','Handler claimed overdue coil move.',UTC_TIMESTAMP() - INTERVAL 75 MINUTE),
    ('a0000000-0013-4000-8000-000000000013','f0000000-0013-4000-8000-000000000013',NULL,'Pending','Created','6229','John Koll','Scrap request created.',UTC_TIMESTAMP() - INTERVAL 3 HOUR),
    ('a0000000-0014-4000-8000-000000000014','f0000000-0013-4000-8000-000000000013','Pending','Accepted','Accepted','6229','John Koll','Scrap accepted.',UTC_TIMESTAMP() - INTERVAL 170 MINUTE),
    ('a0000000-0015-4000-8000-000000000015','f0000000-0013-4000-8000-000000000013','Accepted','Completed','Completed','6229','John Koll','Scrap completed.',UTC_TIMESTAMP() - INTERVAL 120 MINUTE),
    ('a0000000-0016-4000-8000-000000000016','f0000000-0015-4000-8000-000000000015',NULL,'Pending','Created','6229','John Koll','Outside-service request created.',UTC_TIMESTAMP() - INTERVAL 2 HOUR),
    ('a0000000-0017-4000-8000-000000000017','f0000000-0015-4000-8000-000000000015','Pending','Canceled','Canceled','6229','John Koll','Requester canceled own request.',UTC_TIMESTAMP() - INTERVAL 110 MINUTE),
    ('a0000000-0018-4000-8000-000000000018','f0000000-0016-4000-8000-000000000016',NULL,'Pending','Created','5000','Other User','Coil request created.',UTC_TIMESTAMP() - INTERVAL 5 HOUR),
    ('a0000000-0019-4000-8000-000000000019','f0000000-0016-4000-8000-000000000016','Pending','Canceled','Canceled','5000','Other User','Duplicate - canceled by requester.',UTC_TIMESTAMP() - INTERVAL 280 MINUTE);

-- Seed: seed_mock_master_default
-- Engine: MySQL 5.7
USE mtm_waitlist;
SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE mock_master_tables_registry;
INSERT INTO mock_master_tables_registry
    (public_id, table_name, ui_display_name, description_text, is_active, sort_rank, created_utc, updated_utc)
VALUES
    (UUID(), 'mock_parts', 'Mock Parts', 'The list of part numbers the app uses for testing. Coils start with MMC, flatstock with MMF, and Other items use a 23- code.', 1, 10, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_work_orders', 'Mock Work Orders', 'The jobs/work orders the app can attach a request to.', 1, 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_work_centers', 'Mock Work Centers', 'The presses/work centers where requests are made.', 1, 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_locations', 'Mock Inventory Locations', 'Where material is stored on the floor (plant location codes).', 1, 40, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_requesters', 'Mock Requesters', 'The people who can submit requests during testing.', 1, 50, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_inventory_locations', 'Mock Inventory on Hand', 'How much of each part is on hand at each location as a combined weight.', 1, 60, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'mock_request_types', 'Mock Request Types', 'The kinds of requests that can be created.', 1, 70, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_parts;
INSERT INTO mock_parts
    (public_id, part_number, part_category, part_description, unit_of_measure, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'MMC0001000', 'Coil', 'Primary coil', 'lb', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0000365', 'Coil', 'General coil', 'lb', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMF0005008', 'Flatstock', 'Flat stock sheet', 'each', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '23-0001-0001', 'Other', 'Other item one', 'each', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '23-0002-0002', 'Other', 'Other item two', 'each', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_work_orders;
INSERT INTO mock_work_orders
    (public_id, work_order_number, part_number, work_center_code, is_coil_bearing, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'WO-076951', 'MMC0001000', '100-3', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'WO-076952', 'MMC0000365', '100-6', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'WO-076953', 'MMF0005008', '100-8', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_work_centers;
INSERT INTO mock_work_centers
    (public_id, work_center_code, work_center_description, building, is_active, created_utc, updated_utc)
VALUES
    (UUID(), '100-3', 'Coil Press 100-3', 'Expo Drive', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '100-6', 'Coil Press 100-6', 'Expo Drive', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '100-8', 'Press 100-8', 'Expo Drive', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_locations;
INSERT INTO mock_locations
    (public_id, location_code, location_description, is_ignored, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'V-A0-01', 'Active storage location A0-01', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-A0-04', 'Active storage location A0-04', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-B2-10', 'Active storage location B2-10', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-A0-00', 'Empty location A0-00', 0, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'WC', 'Work cell location (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'NCM', 'Non-conforming material (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'V-WC', 'Work cell variant (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'NCM-VITS', 'Non-conforming VITS (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'SHIP', 'Shipping location (ignored)', 1, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_requesters;
INSERT INTO mock_requesters
    (public_id, employee_number, display_name, is_active, created_utc, updated_utc)
VALUES
    (UUID(), '6229', 'John Koll', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), '5000', 'Other User', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_inventory_locations;
INSERT INTO mock_inventory_locations
    (public_id, part_number, location_code, on_hand_quantity, created_utc, updated_utc)
VALUES
    (UUID(), 'MMC0001000', 'V-A0-01', 15000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-A0-04', 25000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-B2-10', 6000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'WC', 2000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'NCM', 1000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-WC', 500, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'NCM-VITS', 1500, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'SHIP', 4000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMC0001000', 'V-A0-00', 0, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMF0005008', 'V-A0-01', 12000, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'MMF0005008', 'V-A0-04', 8000, UTC_TIMESTAMP(), UTC_TIMESTAMP());

TRUNCATE TABLE mock_request_types;
INSERT INTO mock_request_types
    (public_id, request_type, subtype, is_active, created_utc, updated_utc)
VALUES
    (UUID(), 'Coil', 'Bring', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Coil', 'Pickup', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Pickup', 'Pickup Coil', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Pickup', 'Pickup Flatstock', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Other', NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'Scrap', NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

SET FOREIGN_KEY_CHECKS = 1;

-- Seed: seed_waitlist_request_catalog
-- Engine: MySQL 5.7
USE mtm_waitlist;
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE waitlist_request_subtypes;
TRUNCATE TABLE waitlist_request_types;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_request_types
    (public_id, request_type, control, flow, requires_text_input, prompt_text, min_length, max_length, default_image_path, center_data_grid_fields_json, is_active, created_utc, updated_utc)
VALUES
    ('7bb056da-2dfd-4da5-824c-cff0973544fb', 'Pickup', 'MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Part','Quantity to move','Pickup location','Destination','Traceability ID'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('8ee9f259-e404-4d4f-8f20-b5dd7c1c220f', 'Other', 'MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Request description','Requested work center'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('20f434cb-59f2-4ecb-a623-84ff5fa3bed1', 'Coil', 'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('90dc8c5b-6a66-4cd4-94c1-5fc634363f5d', 'Scrap', 'MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('805c5b0f-815f-46bf-b5a7-73de5d74fa1f', 'Flatstock', 'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Part','Quantity','Work center','Destination'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('b0fc9058-6c74-4171-9f46-11d9b4332b51', 'Table Handling', 'MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Part','Quantity','Work center','Destination'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('be310bec-a74d-4242-a1a8-6220557d8700', 'Die Handling', 'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, JSON_ARRAY('Die','Quantity','Pickup location','Destination'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('11a182a7-507c-4069-9763-b902bc7fe8a0', 'Forklift Assist', 'MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist.ForkliftAssistRequestTypeImageView', 'collect-input-then-confirm', 1, 'Enter description of why you need assistance', 5, 50, NULL, JSON_ARRAY('Description','Work center','Requested by'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT INTO waitlist_request_subtypes
    (public_id, request_type_id, subtype_name, control, flow, requires_text_input, prompt_text, min_length, max_length, default_image_path, center_data_grid_fields_json, is_active, created_utc, updated_utc)
SELECT s.public_id, rt.id, s.subtype_name, s.control, s.flow, s.requires_text_input, s.prompt_text, s.min_length, s.max_length, s.default_image_path, s.center_data_grid_fields_json, s.is_active, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    SELECT 'feaa6b5e-13db-4ad8-9c17-188f40ca41ed' AS public_id, '7bb056da-2dfd-4da5-824c-cff0973544fb' AS parent_public_id, 'Pickup Other' AS subtype_name, 'MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView' AS control, 'collect-input-then-confirm' AS flow, 1 AS requires_text_input, 'Enter what is needed' AS prompt_text, 5 AS min_length, 200 AS max_length, NULL AS default_image_path, JSON_ARRAY('Request description','Requested work center') AS center_data_grid_fields_json, 1 AS is_active
    UNION ALL SELECT '1a50ee24-6959-4242-9853-9b0e7ab19074','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup NCM','MTM_Waitlist.Module_Waitlist.Controls.PickupNcm.PickupNcmWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity to move','Pickup location','Destination','Traceability ID'),1
    UNION ALL SELECT '7bc50583-6bc1-4ffc-94e3-40f2a21ae6d7','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup WIP','MTM_Waitlist.Module_Waitlist.Controls.PickupWip.PickupWipWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part and quantity','Pickup work center','WIP destination','Operation sequence'),1
    UNION ALL SELECT 'efb993e9-be75-4059-a619-be67519c9bc5','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup FG','MTM_Waitlist.Module_Waitlist.Controls.PickupFg.PickupFgWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part number','Quantity remaining','Part description','Customer','Packlist'),1
    UNION ALL SELECT 'b46bab54-8067-436f-ac8b-d0531538422e','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup Coil','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT 'f6113875-95ba-4469-959f-c91979266596','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup Flatstock','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '3be33961-67ad-4fb8-9da1-937996d8bccb','8ee9f259-e404-4d4f-8f20-b5dd7c1c220f','General Text Entry','MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView','collect-input-then-confirm',1,'Enter a short description',5,200,NULL,JSON_ARRAY('Request description','Requested work center'),1
    UNION ALL SELECT '8c5dce16-b806-4165-b314-2368bb97f6d7','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Bring','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '340966b0-d6a3-45d9-890e-21a01a3ad95d','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Pickup','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '9499cd7c-a8d0-4f5f-b1b7-8f5c2b03c258','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Wrong Coil @ press','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','collect-input-then-confirm',1,'Explain how or why the coil is wrong',5,200,NULL,JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '8048b55c-02e8-4797-8c9e-a35fda586650','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Need Riser Table','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '4ad1825a-4fa5-4f9f-8ed3-0cc13f4de1bd','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Need Coil Turned around','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '73371828-06ce-49f1-9480-541e2498dc5d','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Empty','MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT '185903aa-6f85-4c90-a31e-30e1fa855d0e','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Pickup Hopper, do not return','MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT 'bab039e8-ea41-47af-a332-a2853f2148e0','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Bring Hopper','MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT 'bb93c8a1-3921-4e8b-8874-10b9e5215bb3','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Bring','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT 'ccb5ebd3-df70-4405-992f-4632377059a5','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Pickup','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '4d8f6d3b-6116-4d07-bd4a-3cc420b8001f','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Wrong Flatstock @ Workcenter','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT 'dcbcc811-b52d-4b2f-810d-cc34200e2825','b0fc9058-6c74-4171-9f46-11d9b4332b51','Table Place Parts','MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '358870f2-e866-4e12-bd8a-e42f74b784f4','b0fc9058-6c74-4171-9f46-11d9b4332b51','Table Remove Parts','MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '20108458-386b-4337-a3c8-ba2e0481d930','be310bec-a74d-4242-a1a8-6220557d8700','Bring Die','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '20bc250d-4c54-4afb-a678-7190b919ada3','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Put Away','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '36652f24-b2da-47d2-84ca-caaff1a608c4','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Take to Die Shop','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '53ec191c-f74c-4aba-9f15-55ef3c9f5cc5','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Leave @ press','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
) AS s
JOIN waitlist_request_types rt ON rt.public_id = s.parent_public_id;
