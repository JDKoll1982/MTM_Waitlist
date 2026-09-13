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
    ),
    (
        UUID(),
        'admin',
        'Admin',
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
    ),
    -- One masked account per user type (T157), so every service-operator authorization branch can be
    -- exercised against a real store instead of only the tests' stub resolver. The credential is the
    -- placeholder the accounts above use, so none of these can log in; the operator-role lookup the
    -- service performs needs identity and role only (`sp_auth_user_row_get`).
    (UUID(), 'test.admin', '0000', NULL, 1, 'Test Admin', '9001', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.developer', '0000', NULL, 1, 'Test Developer', '9002', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.plant.manager', '0000', NULL, 1, 'Test Plant Manager', '9003', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.setup.lead', '0000', NULL, 1, 'Test Setup Lead', '9004', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.production.lead', '0000', NULL, 1, 'Test Production Lead', '9005', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    -- Deliberately NOT approved operator roles: these are the accounts that prove the refusal branch.
    (UUID(), 'test.setup', '0000', NULL, 1, 'Test Setup', '9006', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.production', '0000', NULL, 1, 'Test Production', '9007', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.material.handler', '0000', NULL, 1, 'Test Material Handler', '9008', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP())
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
    INNER JOIN auth_roles_catalog roles
        ON roles.role_code = CASE users.username_normalized
            WHEN 'johnk' THEN 'developer'
            WHEN 'jkoll' THEN 'developer'
            WHEN 'test.admin' THEN 'admin'
            WHEN 'test.developer' THEN 'developer'
            WHEN 'test.plant.manager' THEN 'plant_manager'
            WHEN 'test.setup.lead' THEN 'setup_lead'
            WHEN 'test.production.lead' THEN 'production_lead'
            WHEN 'test.setup' THEN 'setup'
            WHEN 'test.production' THEN 'production'
            WHEN 'test.material.handler' THEN 'material_handler'
        END
WHERE
    users.username_normalized IN (
        'johnk',
        'jkoll',
        'test.admin',
        'test.developer',
        'test.plant.manager',
        'test.setup.lead',
        'test.production.lead',
        'test.setup',
        'test.production',
        'test.material.handler'
    )
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
-- Source: the demo dataset for the Waitlist work, re-keyed for the Category/Item vocabulary
--   (specs/004-unified-card-item-picker). Every stale reference to a request type or a subtype is gone: a
--   seeded request carries the Category and the Item the requester chose, and nothing else identifies it.
--   01 lifecycle (Pending/Accepted/Completed/Canceled + requested/accepted/completed/canceled + note)
--   02 coil requests and part variety for the coil card
--   03 list/detail across several Items, so the one card shape is exercised on more than one kind of thing
--   04 requester cancel-own + "My Requests" (rows from the signed-in requester 6229 AND other requester 5000)
--   05/06 ignored-locations + location grid are NOT persisted in this MySQL table (mock/Infor Visual), so they
--       are exercised by the coil/request rows above.
--   Completed population: enough Items complete, with both endpoints, that
--   sp_waitlist_request_item_observed_average_get has a non-empty population for more than one Item (SC-008):
--   pickup-scrap, pickup-fg, assist-coil-turn and deliver-flatstock all carry an accepted -> completed pair.
-- Idempotent: TRUNCATE both tables first (this is the seed's own demo set), then insert deterministic rows.
-- Consumed by sp_waitlist_request_list / _get / _status_update / _audit_insert, the observed-average read and
-- the Waitlist UI.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE waitlist_requests_audit;
TRUNCATE TABLE waitlist_requests_queue;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_requests_queue (
    public_id, building, work_center, category, item, input_value, active_setup_job_id,
    work_center_name, requester_employee_number, requester_employee_name, status,
    requested_utc, target_time_utc, is_overdue, assigned_material_handler,
    cancellation_reason, canceled_utc, canceled_by_employee_number,
    note, accepted_utc, completed_utc, released_utc, created_utc, updated_utc
)
VALUES
    -- Pending (open) -> returned by sp_waitlist_request_list; drives list/card + wait time + status badge
    ('f0000000-0001-4000-8000-000000000001','Expo Drive','100-3','Pickup','pickup-coil',NULL,'100-3','100-3','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 35 MINUTE, (UTC_TIMESTAMP() - INTERVAL 35 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        'Coil pickup for press 100-3.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 35 MINUTE, UTC_TIMESTAMP() - INTERVAL 35 MINUTE),
    ('f0000000-0002-4000-8000-000000000002','Expo Drive','100-3','Deliver','deliver-wrong-coil','Wrong material at press - expected MMC0001000.','100-3','100-3','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 12 MINUTE, (UTC_TIMESTAMP() - INTERVAL 12 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        'Reports the staged coil is the wrong one.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 12 MINUTE, UTC_TIMESTAMP() - INTERVAL 12 MINUTE),
    ('f0000000-0003-4000-8000-000000000003','Expo Drive','100-6','Pickup','pickup-wip',NULL,'100-6','100-6','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 50 MINUTE, (UTC_TIMESTAMP() - INTERVAL 50 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 50 MINUTE, UTC_TIMESTAMP() - INTERVAL 50 MINUTE),
    ('f0000000-0004-4000-8000-000000000004','Expo Drive','100-5','Pickup','pickup-fg',NULL,'100-5','100-5','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 22 MINUTE, (UTC_TIMESTAMP() - INTERVAL 22 MINUTE) + INTERVAL 45 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 22 MINUTE, UTC_TIMESTAMP() - INTERVAL 22 MINUTE),
    ('f0000000-0005-4000-8000-000000000005','Expo Drive','100-11','Pickup','pickup-ncm',NULL,'100-11','100-11','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 18 MINUTE, (UTC_TIMESTAMP() - INTERVAL 18 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 18 MINUTE, UTC_TIMESTAMP() - INTERVAL 18 MINUTE),
    ('f0000000-0006-4000-8000-000000000006','Expo Drive','100-12','Pickup','pickup-outside-service',NULL,'100-12','100-12','6229','John Koll','Pending',
        UTC_TIMESTAMP() - INTERVAL 9 MINUTE, (UTC_TIMESTAMP() - INTERVAL 9 MINUTE) + INTERVAL 60 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 9 MINUTE, UTC_TIMESTAMP() - INTERVAL 9 MINUTE),
    -- Other requester (5000) open requests -> excluded from signed-in user's "My Requests"; cancel-own by 6229 must be denied
    ('f0000000-0007-4000-8000-000000000007','Expo Drive','100-14','Pickup','pickup-wip',NULL,'100-14','100-14','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 30 MINUTE, (UTC_TIMESTAMP() - INTERVAL 30 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 30 MINUTE, UTC_TIMESTAMP() - INTERVAL 30 MINUTE),
    ('f0000000-0008-4000-8000-000000000008','Expo Drive','100-15','Other','other','Need a line supervisor for a quick walkthrough.','100-15','100-15','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 14 MINUTE, (UTC_TIMESTAMP() - INTERVAL 14 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 14 MINUTE, UTC_TIMESTAMP() - INTERVAL 14 MINUTE),
    ('f0000000-0009-4000-8000-000000000009','Vits Drive','V100-40','Other','other','Need a forklift to move a pallet to staging.','V100-40','V100-40','5000','Other User','Pending',
        UTC_TIMESTAMP() - INTERVAL 26 MINUTE, (UTC_TIMESTAMP() - INTERVAL 26 MINUTE) + INTERVAL 30 MINUTE, 0, NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 26 MINUTE, UTC_TIMESTAMP() - INTERVAL 26 MINUTE),
    -- Accepted (In Progress) -> returned by list; claimed by a handler; lifecycle accepted_utc
    ('f0000000-0010-4000-8000-000000000010','Expo Drive','100-7','Pickup','pickup-coil',NULL,'100-7','100-7','6229','John Koll','Accepted',
        UTC_TIMESTAMP() - INTERVAL 80 MINUTE, (UTC_TIMESTAMP() - INTERVAL 80 MINUTE) + INTERVAL 30 MINUTE, 1, '6229', NULL, NULL, NULL,
        'Overdue coil move in progress.', UTC_TIMESTAMP() - INTERVAL 75 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 80 MINUTE, UTC_TIMESTAMP() - INTERVAL 75 MINUTE),
    ('f0000000-0011-4000-8000-000000000011','Expo Drive','100-8','Other','other','Changeover review requested.','100-8','100-8','6229','John Koll','Accepted',
        UTC_TIMESTAMP() - INTERVAL 25 MINUTE, (UTC_TIMESTAMP() - INTERVAL 25 MINUTE) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 20 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 25 MINUTE, UTC_TIMESTAMP() - INTERVAL 20 MINUTE),
    ('f0000000-0012-4000-8000-000000000012','Expo Drive','100-16','Pickup','pickup-ncm',NULL,'100-16','100-16','5000','Other User','Accepted',
        UTC_TIMESTAMP() - INTERVAL 40 MINUTE, (UTC_TIMESTAMP() - INTERVAL 40 MINUTE) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 33 MINUTE, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 40 MINUTE, UTC_TIMESTAMP() - INTERVAL 33 MINUTE),
    -- Completed (Done) -> resolved, retained; accepted_utc + completed_utc
    ('f0000000-0013-4000-8000-000000000013','Expo Drive','100-9','Pickup','pickup-scrap','Scrapped punch slugs at press 100-9.','100-9','100-9','6229','John Koll','Completed',
        UTC_TIMESTAMP() - INTERVAL 3 HOUR, (UTC_TIMESTAMP() - INTERVAL 3 HOUR) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        'Lugger filled and confirmed.', UTC_TIMESTAMP() - INTERVAL 170 MINUTE, UTC_TIMESTAMP() - INTERVAL 120 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 3 HOUR, UTC_TIMESTAMP() - INTERVAL 120 MINUTE),
    ('f0000000-0014-4000-8000-000000000014','Vits Drive','V100-41','Pickup','pickup-fg',NULL,'V100-41','V100-41','5000','Other User','Completed',
        UTC_TIMESTAMP() - INTERVAL 4 HOUR, (UTC_TIMESTAMP() - INTERVAL 4 HOUR) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 230 MINUTE, UTC_TIMESTAMP() - INTERVAL 150 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 4 HOUR, UTC_TIMESTAMP() - INTERVAL 150 MINUTE),
    -- Canceled -> cancel-own path (file 04): creator cancels own Waiting request; canceled_by = creator
    ('f0000000-0015-4000-8000-000000000015','Expo Drive','100-19','Pickup','pickup-outside-service',NULL,'100-19','100-19','6229','John Koll','Canceled',
        UTC_TIMESTAMP() - INTERVAL 2 HOUR, (UTC_TIMESTAMP() - INTERVAL 2 HOUR) + INTERVAL 30 MINUTE, 0, NULL,
        'Operator no longer needs this pickup.', UTC_TIMESTAMP() - INTERVAL 110 MINUTE, '6229',
        'Canceled by requester before handling.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 2 HOUR, UTC_TIMESTAMP() - INTERVAL 110 MINUTE),
    ('f0000000-0016-4000-8000-000000000016','Expo Drive','100-20','Pickup','pickup-coil',NULL,'100-20','100-20','5000','Other User','Canceled',
        UTC_TIMESTAMP() - INTERVAL 5 HOUR, (UTC_TIMESTAMP() - INTERVAL 5 HOUR) + INTERVAL 30 MINUTE, 0, NULL,
        'Duplicate of an earlier request.', UTC_TIMESTAMP() - INTERVAL 280 MINUTE, '5000',
        'Other requester canceled own request.', NULL, NULL, NULL, UTC_TIMESTAMP() - INTERVAL 5 HOUR, UTC_TIMESTAMP() - INTERVAL 280 MINUTE),
    -- Two more completed requests on in-scope Items, so the observed average has a real population on more
    -- than one Item (SC-008): assist-coil-turn at 50 minutes and deliver-flatstock at 70 minutes.
    ('f0000000-0017-4000-8000-000000000017','Expo Drive','100-3','Assist','assist-coil-turn',NULL,'100-3','100-3','6229','John Koll','Completed',
        UTC_TIMESTAMP() - INTERVAL 6 HOUR, (UTC_TIMESTAMP() - INTERVAL 6 HOUR) + INTERVAL 30 MINUTE, 0, '6229', NULL, NULL, NULL,
        'Coil turned and back on the press.', UTC_TIMESTAMP() - INTERVAL 350 MINUTE, UTC_TIMESTAMP() - INTERVAL 300 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 6 HOUR, UTC_TIMESTAMP() - INTERVAL 300 MINUTE),
    ('f0000000-0018-4000-8000-000000000018','Expo Drive','100-5','Deliver','deliver-flatstock',NULL,'100-5','100-5','5000','Other User','Completed',
        UTC_TIMESTAMP() - INTERVAL 7 HOUR, (UTC_TIMESTAMP() - INTERVAL 7 HOUR) + INTERVAL 30 MINUTE, 0, 'M. Lewis', NULL, NULL, NULL,
        NULL, UTC_TIMESTAMP() - INTERVAL 410 MINUTE, UTC_TIMESTAMP() - INTERVAL 340 MINUTE, NULL, UTC_TIMESTAMP() - INTERVAL 7 HOUR, UTC_TIMESTAMP() - INTERVAL 340 MINUTE);

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

-- Seed: seed_waitlist_request_catalog
-- Engine: MySQL 5.7
-- Canonical mapping (2026-09-08): category + item_id on leaf rows only.
USE mtm_waitlist;
SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE waitlist_request_subtypes;
TRUNCATE TABLE waitlist_request_types;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_request_types
    (public_id, request_type, control, flow, requires_text_input, prompt_text, min_length, max_length, default_image_path, category, item_id, center_data_grid_fields_json, is_active, created_utc, updated_utc)
VALUES
    ('7bb056da-2dfd-4da5-824c-cff0973544fb', 'Pickup', 'MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Part','Quantity to move','Pickup location','Destination','Traceability ID'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('8ee9f259-e404-4d4f-8f20-b5dd7c1c220f', 'Other', 'MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Request description','Requested work center'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('20f434cb-59f2-4ecb-a623-84ff5fa3bed1', 'Coil', 'MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('90dc8c5b-6a66-4cd4-94c1-5fc634363f5d', 'Scrap', 'MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('805c5b0f-815f-46bf-b5a7-73de5d74fa1f', 'Flatstock', 'MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Part','Quantity','Work center','Destination'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('b0fc9058-6c74-4171-9f46-11d9b4332b51', 'Table Handling', 'MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Part','Quantity','Work center','Destination'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('be310bec-a74d-4242-a1a8-6220557d8700', 'Die Handling', 'MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView', 'direct-to-confirmation', 0, NULL, 0, 200, NULL, NULL, NULL, JSON_ARRAY('Die','Quantity','Pickup location','Destination'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('11a182a7-507c-4069-9763-b902bc7fe8a0', 'Forklift Assist', 'MTM_Waitlist.Module_Waitlist.Controls.ForkliftAssist.ForkliftAssistRequestTypeImageView', 'collect-input-then-confirm', 1, 'Enter description of why you need assistance', 5, 50, NULL, 'Other', 'other', JSON_ARRAY('Description','Work center','Requested by'), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT INTO waitlist_request_subtypes
    (public_id, request_type_id, subtype_name, control, flow, requires_text_input, prompt_text, min_length, max_length, default_image_path, category, item_id, center_data_grid_fields_json, is_active, created_utc, updated_utc)
SELECT s.public_id, rt.id, s.subtype_name, s.control, s.flow, s.requires_text_input, s.prompt_text, s.min_length, s.max_length, s.default_image_path, s.category, s.item_id, s.center_data_grid_fields_json, s.is_active, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    SELECT 'feaa6b5e-13db-4ad8-9c17-188f40ca41ed' AS public_id, '7bb056da-2dfd-4da5-824c-cff0973544fb' AS parent_public_id, 'Pickup Other' AS subtype_name, 'MTM_Waitlist.Module_Waitlist.Controls.Pickup.PickupRequestTypeImageView' AS control, 'collect-input-then-confirm' AS flow, 1 AS requires_text_input, 'Enter what is needed' AS prompt_text, 5 AS min_length, 200 AS max_length, NULL AS default_image_path, 'Other' AS category, 'other' AS item_id, JSON_ARRAY('Request description','Requested work center') AS center_data_grid_fields_json, 1 AS is_active
    UNION ALL SELECT '1a50ee24-6959-4242-9853-9b0e7ab19074','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup NCM','MTM_Waitlist.Module_Waitlist.Controls.PickupNcm.PickupNcmWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-ncm',JSON_ARRAY('Part','Quantity to move','Pickup location','Destination','Traceability ID'),1
    UNION ALL SELECT '7bc50583-6bc1-4ffc-94e3-40f2a21ae6d7','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup WIP','MTM_Waitlist.Module_Waitlist.Controls.PickupWip.PickupWipWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-wip',JSON_ARRAY('Part and quantity','Pickup work center','WIP destination','Operation sequence'),1
    UNION ALL SELECT 'efb993e9-be75-4059-a619-be67519c9bc5','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup FG','MTM_Waitlist.Module_Waitlist.Controls.PickupFg.PickupFgWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-fg',JSON_ARRAY('Part number','Quantity remaining','Part description','Customer','Packlist'),1
    UNION ALL SELECT 'b46bab54-8067-436f-ac8b-d0531538422e','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup Coil','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-coil',JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT 'f6113875-95ba-4469-959f-c91979266596','7bb056da-2dfd-4da5-824c-cff0973544fb','Pickup Flatstock','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-coil',JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '3be33961-67ad-4fb8-9da1-937996d8bccb','8ee9f259-e404-4d4f-8f20-b5dd7c1c220f','General Text Entry','MTM_Waitlist.Module_Waitlist.Controls.Other.OtherRequestTypeImageView','collect-input-then-confirm',1,'Enter a short description',5,200,NULL,'Other','other',JSON_ARRAY('Request description','Requested work center'),1
    UNION ALL SELECT '8c5dce16-b806-4165-b314-2368bb97f6d7','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Bring','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Deliver','deliver-coil',JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '340966b0-d6a3-45d9-890e-21a01a3ad95d','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Pickup','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-coil',JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '9499cd7c-a8d0-4f5f-b1b7-8f5c2b03c258','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Wrong Coil @ press','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','collect-input-then-confirm',1,'Explain how or why the coil is wrong',5,200,NULL,'Deliver','deliver-wrong-coil',JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '8048b55c-02e8-4797-8c9e-a35fda586650','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Need Riser Table','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Deliver','deliver-riser-table',JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '4ad1825a-4fa5-4f9f-8ed3-0cc13f4de1bd','20f434cb-59f2-4ecb-a623-84ff5fa3bed1','Need Coil Turned around','MTM_Waitlist.Module_Waitlist.Controls.Coil.CoilWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Assist','assist-coil-turn',JSON_ARRAY('Coil number','Quantity in house','Coil description','Average coil weight','Requesting work center'),1
    UNION ALL SELECT '73371828-06ce-49f1-9480-541e2498dc5d','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Empty','MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-scrap',JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT '185903aa-6f85-4c90-a31e-30e1fa855d0e','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Pickup Hopper, do not return','MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-hopper',JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT 'bab039e8-ea41-47af-a332-a2853f2148e0','90dc8c5b-6a66-4cd4-94c1-5fc634363f5d','Bring Hopper','MTM_Waitlist.Module_Waitlist.Controls.Scrap.ScrapWaitlistLineView','direct-to-confirmation',0,NULL,0,200,NULL,'Deliver','deliver-hopper',JSON_ARRAY('Pickup work center','Quantity involved','Scrap lugger','Scrap reason'),1
    UNION ALL SELECT 'bb93c8a1-3921-4e8b-8874-10b9e5215bb3','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Bring','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Deliver','deliver-flatstock',JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT 'ccb5ebd3-df70-4405-992f-4632377059a5','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Pickup','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-coil',JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '4d8f6d3b-6116-4d07-bd4a-3cc420b8001f','805c5b0f-815f-46bf-b5a7-73de5d74fa1f','Wrong Flatstock @ Workcenter','MTM_Waitlist.Module_Waitlist.Controls.Flatstock.FlatstockRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Deliver','deliver-wrong-flatstock',JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT 'dcbcc811-b52d-4b2f-810d-cc34200e2825','b0fc9058-6c74-4171-9f46-11d9b4332b51','Table Place Parts','MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Assist','assist-table-place',JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '358870f2-e866-4e12-bd8a-e42f74b784f4','b0fc9058-6c74-4171-9f46-11d9b4332b51','Table Remove Parts','MTM_Waitlist.Module_Waitlist.Controls.TableHandling.TableHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Assist','assist-table-remove',JSON_ARRAY('Part','Quantity','Work center','Destination'),1
    UNION ALL SELECT '20108458-386b-4337-a3c8-ba2e0481d930','be310bec-a74d-4242-a1a8-6220557d8700','Bring Die','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Deliver','deliver-die',JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '20bc250d-4c54-4afb-a678-7190b919ada3','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Put Away','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-die',JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '36652f24-b2da-47d2-84ca-caaff1a608c4','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Take to Die Shop','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-die',JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
    UNION ALL SELECT '53ec191c-f74c-4aba-9f15-55ef3c9f5cc5','be310bec-a74d-4242-a1a8-6220557d8700','Pull Die and Leave @ press','MTM_Waitlist.Module_Waitlist.Controls.DieHandling.DieHandlingRequestTypeImageView','direct-to-confirmation',0,NULL,0,200,NULL,'Pickup','pickup-die',JSON_ARRAY('Die','Quantity','Pickup location','Destination'),1
) AS s
JOIN waitlist_request_types rt ON rt.public_id = s.parent_public_id;

-- Seed: seed_waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Purpose: Populate waitlist_request_item_configs with one row for every one of the twenty-three catalogued
--          Items, so a missing configuration row is the exception rather than the normal case and FR-014's
--          "every Item has a configuration row" is true of the shipped data.
-- Design-time sources (FR-027): the per-Item field definitions and the retiring type/subtype catalog, whose
--          populated category / item_id columns map each legacy leaf onto its canonical Item. Both are read
--          once, by a person, when this file is authored. The application never reads either document, and
--          Database/Mock/... and the build carry no reference to them.
-- Rerunnable: TRUNCATE first, so the file is deterministic and the table matches this file exactly. A
--          reinstall therefore returns every Item to its seeded allotment, which is the intended behaviour of
--          a database reinstalled from seed.
-- Notes on individual rows:
--   * The four out-of-scope Items (pickup-fg, pickup-ncm, pickup-wip, pickup-outside-service) are seeded like
--     every other Item and hidden by the visibility rules, not by deletion: a hidden Item and a missing row
--     are different states and must not be conflated (FR-028).
--   * assist-table-remove deliberately carries NO configured allotment, so the labelled 15-minute default is
--     real data rather than a hypothetical one.
--   * pickup-component's options are the requesting job's component list, so its options_json is NULL: the
--     enumerated values arrive with the job snapshot rather than from configuration. pickup-die's options are
--     fixed and are seeded here.
--   * detail_fields_json's key order stays inside the JSON payload: a bare `order` column is banned by
--     Database/Database-Ruleset.md and is never introduced.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE waitlist_request_item_configs;

SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO waitlist_request_item_configs
    (public_id, item, category, control_flow, requires_answer, answer_value_type, prompt_text,
     min_length, max_length, options_json, detail_fields_json, allotted_minutes,
     created_utc, updated_utc)
VALUES
-- ------------------------------------------------------------------ Pickup (11)
('c1000000-0000-4000-8000-000000000001', 'pickup-coil', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Coil description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Average coil weight','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',5,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000002', 'pickup-die', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Choose where the die is going.', 0, 200,
 JSON_ARRAY('Die Shop', 'Home Location', 'Other'),
 JSON_ARRAY(
   JSON_OBJECT('label','Die','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','enum','source','answer','order',4,'is_required',TRUE)),
 45, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000003', 'pickup-component', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Choose the component to collect.', 0, 200,
 NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Component','value_type','enum','source','answer','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000004', 'pickup-fg', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity remaining','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Customer','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Packlist','value_type','string','source','job','order',5,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000005', 'pickup-ncm', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity to move','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Traceability ID','value_type','string','source','job','order',5,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000006', 'pickup-wip', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part and quantity','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',2,'is_required',FALSE),
   JSON_OBJECT('label','WIP destination','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Operation sequence','value_type','string','source','job','order',4,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000007', 'pickup-outside-service', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity to move','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Traceability ID','value_type','string','source','job','order',5,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000008', 'pickup-riser-table', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000009', 'pickup-dunnage', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Dunnage part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Home location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Quantity on hand','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000010', 'pickup-scrap', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Scrap type','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity involved','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Scrap lugger','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000011', 'pickup-hopper', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Deliver (8)
('c1000000-0000-4000-8000-000000000012', 'deliver-coil', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Coil description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Average coil weight','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',5,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000013', 'deliver-riser-table', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000014', 'deliver-hopper', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000015', 'deliver-flatstock', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000016', 'deliver-die', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Die','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 45, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000017', 'deliver-dunnage', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Dunnage part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Home location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Quantity on hand','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000018', 'deliver-wrong-coil', 'Deliver', 'collect-input-then-confirm', 1, 'text',
 'Explain how or why the coil is wrong', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Reason','value_type','text','source','answer','order',2,'is_required',TRUE),
   JSON_OBJECT('label','Correct coil','value_type','string','source','job','order',3,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000019', 'deliver-wrong-flatstock', 'Deliver', 'collect-input-then-confirm', 1, 'text',
 'Explain how or why the flatstock is wrong', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Reason','value_type','text','source','answer','order',2,'is_required',TRUE),
   JSON_OBJECT('label','Correct flatstock','value_type','string','source','job','order',3,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Assist (3)
('c1000000-0000-4000-8000-000000000020', 'assist-coil-turn', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000021', 'assist-table-place', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000022', 'assist-table-remove', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','job','order',4,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Other (1)
('c1000000-0000-4000-8000-000000000023', 'other', 'Other', 'collect-input-then-confirm', 1, 'text',
 'Enter a short description', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Request description','value_type','text','source','answer','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Requested work center','value_type','string','source','fixed','order',2,'is_required',FALSE)),
 15, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- Seed: seed_waitlist_request_item_images
-- Engine: MySQL 5.7
-- Purpose: Give the card's two-hop picture fallback (FR-009) something real to fall back to, under the two
--          scopes this feature adds to config_images_locations:
--            request_item      keyed by Item code      (the Item's own picture)
--            request_category  keyed by Category code  (the family a card falls back to)
--          Resolution order is Item -> Category family -> the existing placeholder, and a "nothing configured"
--          answer never replaces a picture the request already resolves (FR-009, FR-021).
-- Data, not DDL: scope is a free-form VARCHAR, so adding two values is a row change.
-- What is seeded, and what is deliberately not:
--   * Only paths to pictures that actually ship are seeded: Assets/RequestTypes/*.png, copied into the app
--     output directory alongside the executable. DoesPathExist resolves a relative path against the app
--     directory, so these rows resolve rather than falling through to the placeholder.
--   * The Deliver Category carries no family picture because none ships; it falls to the placeholder, which
--     is the honest answer and exercises the second hop.
--   * No row is seeded for an Item with no picture of its own: an operator adds those through the picture
--     screen, and a seeded row pointing at a file that does not exist would be a lie that resolves to a
--     placeholder anyway.
-- Rerunnable: clears only the two scopes this seed owns, so work-centre and legacy override rows are untouched.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM config_images_locations WHERE scope IN ('request_item', 'request_category');

SET FOREIGN_KEY_CHECKS = 1;

-- --------------------------------------------------------------- request_category: the family a card falls back to
INSERT INTO config_images_locations
    (public_id, scope, scope_item_id, image_path, is_active, created_utc, updated_utc)
VALUES
    ('c2000000-0000-4000-8000-000000000001', 'request_category', 'Pickup',
     'Assets/RequestTypes/pickup.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000002', 'request_category', 'Assist',
     'Assets/RequestTypes/forklift-assist.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000003', 'request_category', 'Other',
     'Assets/RequestTypes/other.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- --------------------------------------------------------------- request_item: the Item's own picture
INSERT INTO config_images_locations
    (public_id, scope, scope_item_id, image_path, is_active, created_utc, updated_utc)
VALUES
    ('c2000000-0000-4000-8000-000000000011', 'request_item', 'pickup-die',
     'Assets/RequestTypes/die-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000012', 'request_item', 'deliver-die',
     'Assets/RequestTypes/die-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000013', 'request_item', 'deliver-flatstock',
     'Assets/RequestTypes/flatstock.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000014', 'request_item', 'assist-table-place',
     'Assets/RequestTypes/table-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000015', 'request_item', 'assist-table-remove',
     'Assets/RequestTypes/table-handling.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    ('c2000000-0000-4000-8000-000000000016', 'request_item', 'other',
     'Assets/RequestTypes/other.png', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

-- Seed: seed_setup_active_jobs_eight_configurations
-- Engine: MySQL 5.7
-- Purpose: Seed the eight active-job configurations the Run 4 visibility matrix is driven through, so the
--          Item picker's rules are proved against the REAL read path (ActiveJobItemResolverService and its own
--          deserializer) rather than against a hand-written expectation.
-- The eight configurations, on the seed's own work-centre block:
--   900-1  coil only            MMC subordinate, a REAL scrap type
--   900-2  flatstock only       MMF subordinate deliberately tagged Component, so the part-number prefix wins
--                               (this is the job that proves a flatstock-only job is offered pickup-coil, D21)
--   900-3  die only             FGT subordinate, scrap value 'Scrap Type Required'
--   900-4  component only       non-prefixed part, stored Category Component, scrap value 'No Scrap'
--   900-5  dunnage only         no subordinate part, an assigned dunnage part
--   900-6  everything at once   coil + flatstock + die + component, a REAL scrap type, and dunnage
--   900-7  no subordinate part  an empty subordinate array, no dunnage
--   (900-8) a work centre with NO active job: realised by the ABSENCE of a row, which is what the resolver
--           reads as "no active job" — the seed inserts nothing for it on purpose.
-- Scrap decisions across the block cover the whole three-way gate (a real type, 'No Scrap',
-- 'Scrap Type Required', and unset), which is the input FR-031 and contract section 5 are written against.
-- Shape: subordinate_parts_json / selected_dunnage_parts_json are written in exactly the shape the write path
-- stores them (SetupPersistenceService serializes the models directly, so the keys are PascalCase property
-- names), because the read path deserializes into SetupSubordinatePart / SetupDunnagePart with the default
-- options. A hand-written shape that merely looked plausible would make this seed a test of itself.
-- Rerunnable: clears its own seven rows by work centre, and nothing else.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM setup_active_jobs
WHERE work_center IN ('900-1', '900-2', '900-3', '900-4', '900-5', '900-6', '900-7');

SET FOREIGN_KEY_CHECKS = 1;

-- --------------------------------------------------------------- 900-1: coil only, a real scrap type
INSERT INTO setup_active_jobs
    (public_id, work_order, part_number, sequence_number, work_center,
     selected_dunnage_type_id, selected_dunnage_part_id,
     subordinate_parts_json, selected_dunnage_parts_json, is_active, created_utc, updated_utc)
VALUES
    ('c3000000-0000-4000-8000-000000000001', 'WO-900001', 'PART-9001', '10', '900-1',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Coil',
        'PartNumber', 'MMC0001000',
        'Description', 'Coil 0.062 x 48 wide',
        'Location', 'A-01-01',
        'OnHandQuantity', 1200.50,
        'User8', '',
        'SelectedScrapType', 'Steel Offal',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-2: flatstock only, prefix wins over the stored tag
    ('c3000000-0000-4000-8000-000000000002', 'WO-900002', 'PART-9002', '10', '900-2',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Component',
        'PartNumber', 'MMF0001154',
        'Description', 'Flatstock 0.125 x 36',
        'Location', 'B-02-03',
        'OnHandQuantity', 88.00,
        'User8', '',
        'SelectedScrapType', 'No Scrap',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-3: die only, the placeholder scrap value
    ('c3000000-0000-4000-8000-000000000003', 'WO-900003', 'PART-9003', '10', '900-3',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Die',
        'PartNumber', 'FGT0002000',
        'Description', 'Die 9003-A',
        'Location', 'DIE SHOP',
        'OnHandQuantity', 0,
        'User8', '',
        'SelectedScrapType', 'Scrap Type Required',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-4: a component, and a real "no scrap" answer
    ('c3000000-0000-4000-8000-000000000004', 'WO-900004', 'PART-9004', '10', '900-4',
     NULL, NULL,
     JSON_ARRAY(JSON_OBJECT(
        'Category', 'Component',
        'PartNumber', 'CMP0004455',
        'Description', 'Bushing, bronze',
        'Location', 'C-04-12',
        'OnHandQuantity', 240.00,
        'User8', '',
        'SelectedScrapType', 'No Scrap',
        'IsLowStock', FALSE)),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-5: no subordinate part, dunnage assigned
    ('c3000000-0000-4000-8000-000000000005', 'WO-900005', 'PART-9005', '10', '900-5',
     'DNG-TYPE-01', 'DNG-PART-01',
     JSON_ARRAY(),
     JSON_ARRAY(JSON_OBJECT(
        'Id', 'DNG-PART-01',
        'TypeId', 'DNG-TYPE-01',
        'PartNumber', 'DNG0007788',
        'DisplayName', 'Rack, 24 x 36 wire',
        'DunnageTypeName', 'Rack',
        'HomeLocation', 'DUNNAGE-ROW-3',
        'ImagePath', '',
        'Metadata', '',
        'IsSelectedForPair', TRUE)),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-6: everything at once
    ('c3000000-0000-4000-8000-000000000006', 'WO-900006', 'PART-9006', '20', '900-6',
     'DNG-TYPE-02', 'DNG-PART-02',
     JSON_ARRAY(
        JSON_OBJECT(
            'Category', 'Coil',
            'PartNumber', 'MMC0001001',
            'Description', 'Coil 0.048 x 36 wide',
            'Location', 'A-01-07',
            'OnHandQuantity', 640.00,
            'User8', '',
            'SelectedScrapType', 'Aluminum Offal',
            'IsLowStock', FALSE),
        JSON_OBJECT(
            'Category', 'Flatstock',
            'PartNumber', 'MMF0001155',
            'Description', 'Flatstock 0.090 x 24',
            'Location', 'B-02-08',
            'OnHandQuantity', 12.00,
            'User8', '',
            'SelectedScrapType', 'No Scrap',
            'IsLowStock', TRUE),
        JSON_OBJECT(
            'Category', 'Die',
            'PartNumber', 'FGT0002001',
            'Description', 'Die 9006-B',
            'Location', 'PRESS BAY',
            'OnHandQuantity', 0,
            'User8', '',
            'SelectedScrapType', 'Scrap Type Required',
            'IsLowStock', FALSE),
        JSON_OBJECT(
            'Category', 'Component',
            'PartNumber', 'CMP0004456',
            'Description', 'Pin, dowel 8mm',
            'Location', 'C-04-15',
            'OnHandQuantity', 0,
            'User8', '',
            'SelectedScrapType', '',
            'IsLowStock', FALSE)),
     JSON_ARRAY(JSON_OBJECT(
        'Id', 'DNG-PART-02',
        'TypeId', 'DNG-TYPE-02',
        'PartNumber', 'DNG0007789',
        'DisplayName', 'Tote, collapsible',
        'DunnageTypeName', 'Tote',
        'HomeLocation', 'DUNNAGE-ROW-4',
        'ImagePath', '',
        'Metadata', '',
        'IsSelectedForPair', TRUE)),
     1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- --------------------------------------------------------------- 900-7: an active job with no subordinate part
    ('c3000000-0000-4000-8000-000000000007', 'WO-900007', 'PART-9007', '10', '900-7',
     NULL, NULL,
     JSON_ARRAY(),
     NULL, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());
