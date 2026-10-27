-- Seed: Image Storage Configuration Overrides
-- Description: Populates config_settings_values with default image storage settings
--              that can be overridden by admins in-app.
-- Table: config_settings_values
-- Scope: global (all_users)

USE mtm_waitlist;

-- Guarded: config_settings_history holds a foreign key back to this table, and MySQL refuses TRUNCATE on a
-- referenced table even when the child is empty. Without the guard the whole seed phase stops here on a fresh
-- store, and every seed after this one in AllSeeds.sql never runs.
SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE config_settings_values;

SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES (
        UUID(),
        'image_storage.shared_folder_path',
        'all_users',
        'all_users',
        'X:\\Software Development\\Live Applications\\MTM_Waitlist\\Images',
        'text',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'image_storage.max_file_size_bytes',
        'all_users',
        'all_users',
        NULL,
        'int',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'image_storage.enable_archive_versioning',
        'all_users',
        'all_users',
        NULL,
        'bool',
        NULL,
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'image_storage.archive_keep_days',
        'all_users',
        'all_users',
        NULL,
        'int',
        NULL,
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    scope_type = VALUES(scope_type),
    scope_key = VALUES(scope_key),
    setting_value = VALUES(setting_value),
    setting_value_int = VALUES(setting_value_int),
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- Dev/test masked baseline seed
-- Do not use real production identity/session data in non-production.
--
-- Roles and rungs (feature 006, FR-015/FR-018): every LIVE role this seed holds carries its rung, and
-- `material_handler_lead` is added as a role in its own right rather than mapped onto Plant Manager. The
-- retired `admin` row is deliberately NOT ranked here and is written by its own statement below, so it holds the
-- column default of 0: that is what makes the role rename's paired reversal determinate, because the reversal
-- re-inserts `admin` "carrying the rung that row held".
--
-- This seed does not remove the retired role, does not insert `it_department`, and does not repoint the masked
-- `test.admin` account. Those belong entirely to `seed_role_admin_to_it_department`, so two seeds never own the
-- same rows and the reversal stays exact for the reason it states.

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE auth_roles_catalog;

INSERT INTO
    auth_roles_catalog (
        public_id,
        role_code,
        role_name,
        role_rank,
        created_utc,
        updated_utc
    )
VALUES (
        UUID(),
        'material_handler',
        'Material Handler',
        10,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'production',
        'Production',
        10,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'production_lead',
        'Production Lead',
        70,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'setup',
        'Setup',
        10,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'setup_lead',
        'Setup Lead',
        60,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'material_handler_lead',
        'Material Handler Lead',
        50,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'plant_manager',
        'Plant Manager',
        80,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    (
        UUID(),
        'developer',
        'Developer',
        100,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    )
ON DUPLICATE KEY UPDATE
    role_name = VALUES(role_name),
    role_rank = VALUES(role_rank),
    updated_utc = VALUES(updated_utc);

-- The retired role, left unranked on purpose so it holds the column default of 0. Its own statement, because a
-- shared column list cannot omit `role_rank` for one row and supply it for the rest.
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
        first_name,
        last_name,
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
        'John',
        'Koll',
        'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=',
        UNHEX('C3B851513BAF5951482CEC1D8D585EBF'),
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
        'John',
        'Koll',
        'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=',
        UNHEX('C3B851513BAF5951482CEC1D8D585EBF'),
        1,
        'John Koll',
        '6229',
        1,
        UTC_TIMESTAMP(),
        UTC_TIMESTAMP()
    ),
    -- One masked account per user type (T157), so every service-operator authorization branch can be
    -- exercised against a real store instead of only the tests' stub resolver. The operator-role lookup the
    -- service performs needs identity and role only (`sp_auth_user_row_get`).
    --
    -- The credential is a development placeholder, salted and hashed the way PasswordSecretHasher.Hash does it,
    -- so a fresh install can be signed into. It used to be written as the bare four-character marker with a null
    -- salt, and PasswordSecretHasher.Verify refuses a null salt, so no comparison could ever confirm it and
    -- every seeded account was unable to sign in anywhere. The flag below still makes it a temporary credential,
    -- so the person is asked to choose a real password at the first sign-in.
    --
    -- Every seeded account deliberately shares this value, and it is visible in source control. It is a
    -- development placeholder rather than a secret, and it belongs to no real person.
    (UUID(), 'test.admin', 'Test', 'Admin', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Admin', '9001', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.developer', 'Test', 'Developer', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Developer', '9002', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.plant.manager', 'Test', 'Plant Manager', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Plant Manager', '9003', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.setup.lead', 'Test', 'Setup Lead', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Setup Lead', '9004', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.production.lead', 'Test', 'Production Lead', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Production Lead', '9005', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    -- Deliberately NOT approved operator roles: these are the accounts that prove the refusal branch.
    (UUID(), 'test.setup', 'Test', 'Setup', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Setup', '9006', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.production', 'Test', 'Production', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Production', '9007', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
    (UUID(), 'test.material.handler', 'Test', 'Material Handler', 'CxrKO1xEmsC1VtDhgp2Envsig5vQ8ZErO8J1g7B7q2o=', UNHEX('C3B851513BAF5951482CEC1D8D585EBF'), 1, 'Test Material Handler', '9008', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    first_name = VALUES(first_name),
    last_name = VALUES(last_name),
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

-- No computers are seeded.
--
-- The registry used to be truncated and given two rows, which meant a freshly built store already had a
-- configured workstation and machine setup never ran. A computer is now registered by the machine itself when
-- somebody completes setup, so the seed leaves the registry empty and the first launch of a fresh store reaches
-- the setup stage as it would on a machine the store has never seen. Nothing here deletes a registered computer
-- either: re-running this seed must not revoke the fleet.

SET FOREIGN_KEY_CHECKS = 1;

-- Seed: seed_plant_buildings_default
-- Engine: MySQL 5.7
--
-- The plant's two buildings, and the default answer for every computer that has not been set up yet.
--
-- A fresh store has no computers at all, so the assignment below writes nothing there: the machine that runs
-- setup registers itself and records its own building at the same time. This block is what gives a store whose
-- computers predate the table its default answer.

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

-- Seed: seed_permission_role_baselines
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T036)
-- Purpose: write what each role gives every one of its people, as data rather than as values fixed in the code
--          (FR-046, FR-047, FR-048, FR-051, FR-052, SC-004, §6 gate 7).
--
-- What a row is
--   One `role`-scoped row in `config_settings_values` per permission per role, keyed `role_type = 'role'`,
--   `scope_key = 'role:<role_code>'`, `value_type = 'bool'`. That is exactly the shape
--   `sp_config_permissions_user_get` matches on, so a role's answer is inherited by every one of its people
--   without a row being written for any of them. No `user`-scoped row is written here: the first per-person row
--   in this feature is written by `sp_config_permissions_user_set` (T054), after the scope re-rank (T006) has
--   shipped, which is the ordering the tasks file pins.
--
-- Why the answers are what they are
--   The eleven keys that replace a hand-written role list carry today's membership of that list, so nobody's
--   access changes on ship day (FR-052). The retired display names were mapped onto the nine role codes the
--   catalogue holds after the rename, and two entries that match no catalogue role were dropped rather than
--   carried forward:
--     * `Admin` and `admin` become `it_department`, which is the role the rename put in the retired role's place.
--       This seed therefore keys to `it_department` and never to `admin`; that is why T036 runs after Phase 4.
--     * `administrator` (DefectTypeCatalogService.AllowedRoles) and `Setup Tech`
--       (SetupWorkCenterViewModel.AllowedManageRoles) match no role the catalogue has ever held and are dropped,
--       so a plain Setup person is not given work-centre setup by default (FR-107, spec Assumptions).
--
--   Three keys have no predecessor list, so their answers are stated rather than compared:
--     * `permission.admin.users` and `permission.admin.reset_password` ship to Production Lead, Setup Lead,
--       Material Handler Lead, Plant Manager, IT Department and Developer (decisions 3 and 8 together).
--     * `permission.admin.permissions` ships to IT Department, Plant Manager and Developer only, and is fixed
--       rather than editable (decision 9).
--
--   Four keys were added later by 010-startup-rebuild (T053) for the privileged surfaces the rebuilt launch
--   introduces, and they are stated too, because no retired list could describe a key that did not exist when the
--   lists were written: `permission.settings.machine_configuration`, `permission.settings.ignored_locations_edit`
--   and `permission.settings.session_length` ship to IT Department and Developer, and
--   `permission.settings.log_panel` to Developer alone. Their nine rows each are at the end of this file.
--
--   `material_handler_lead` appears in none of the retired lists, so its whole row is stated: the worker level,
--   plus handling requests and the service cache refresh, and nothing else. It is deliberately NOT granted the
--   ignored-locations feature although a plain worker in a production role is. Nobody holds the role on ship
--   day, so this changes nothing for anybody (spec Assumptions).
--
-- The one subset rule this data must satisfy
--   Whatever `permission.settings.cache_refresh` admits (`developer`, `it_department`, `plant_manager`) must also
--   be admitted by `permission.cache.refresh_api`. Read the two rows below together before changing either.
--
-- Re-running
--   Every statement is `ON DUPLICATE KEY UPDATE` against the table's `(setting_key, scope_key)` unique key, so
--   re-running is idempotent and an operator's later edit survives a re-run of the value they changed only if
--   they change this file. That is the intended direction: the shipped answer is the file, and the durable
--   per-person route is `sp_config_permissions_user_set`.
--
-- Ordering inside AllSeeds.sql
--   This folder sorts after `seed_dev_masked_baseline`, which truncates `config_settings_values`, so the rows
--   survive the seed phase. It sorts before `seed_role_admin_to_it_department`, which is harmless: `scope_key`
--   is free text and carries no foreign key to the role catalogue, so a baseline row may name `it_department`
--   before that row has been inserted.

USE mtm_waitlist;

SET NAMES utf8mb4;

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    -- developer (rung 100) — today's administrator, everything.
    (UUID(), 'permission.requests.handle', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    -- it_department (rung 90) — what the retired `Admin` list gave, so everything.
    (UUID(), 'permission.requests.handle', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    -- plant_manager (rung 80) — no part pictures, no defect catalogue, no computer registry.
    (UUID(), 'permission.requests.handle', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    -- production_lead (rung 70) — the two setup keys and the two account keys, and nothing narrower.
    (UUID(), 'permission.requests.handle', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    -- setup_lead (rung 60) — the same answers as Production Lead.
    (UUID(), 'permission.requests.handle', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    -- material_handler_lead (rung 50) — stated, not compared: the worker level plus handling and the cache API.
    (UUID(), 'permission.requests.handle', 'role', 'role:material_handler_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:material_handler_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:material_handler_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:material_handler_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    -- material_handler (rung 10) — handling only. Not in the ignored-locations list.
    (UUID(), 'permission.requests.handle', 'role', 'role:material_handler', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    -- production (rung 10) — handling and ignored locations.
    (UUID(), 'permission.requests.handle', 'role', 'role:production', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:production', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    -- setup (rung 10) — the same answers as production.
    (UUID(), 'permission.requests.handle', 'role', 'role:setup', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.cache.refresh_api', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations', 'role', 'role:setup', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.hot_work_centers', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.part_pictures', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.cache_refresh', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.urgency_minutes', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.defect_types', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.computers', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.dunnage_quick_add', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.setup.work_centers', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.users', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.reset_password', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.admin.permissions', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- ---------------------------------------------------------------------------
-- permission.settings.storage_paths — the fifteenth key
--
--   Where the application keeps the pictures and the key files. IT Department and Developer own the site's
--   layout, so they alone answer 1: a wrong picture root stops every configured picture being read on every
--   machine, which is not a decision for the screen the pictures happen to feed. The other seven roles answer 0.
--
--   Written as its own statement rather than woven into the nine blocks above so the addition is reviewable on
--   its own, and so the blocks that record what each role inherited from the retired lists stay unedited.
-- ---------------------------------------------------------------------------
INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.storage_paths', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- ---------------------------------------------------------------------------
-- 010-startup-rebuild (task T053) — the four keys the new privileged surfaces read
--
--   Four surfaces the rebuilt launch adds had no key of their own, so each gets one here, role-explicitly: every
--   one of the nine catalogue roles carries a row for every one of the four keys, which is what makes a role
--   without a key a decision recorded in this file rather than an absence a reader has to infer.
--
--     permission.settings.machine_configuration   IT Department and Developer 1, every other role 0   (FR-007)
--     permission.settings.ignored_locations_edit  IT Department and Developer 1, every other role 0   (FR-024)
--     permission.settings.session_length          IT Department and Developer 1, every other role 0   (FR-029)
--     permission.settings.log_panel               Developer 1 alone, every other role 0                (US5, SC-005)
--
--   `permission.settings.ignored_locations` is deliberately NOT among them and its nine rows above are unchanged.
--   Reading the plant-wide list stays with the roles that already held it, which is what keeps FR-030 true; the
--   new `_edit` key carries FR-024's two-role requirement for writing it instead (research D10). `machine_
--   configuration` is likewise not a reuse of `permission.settings.computers`, although the two have the same
--   role set: machine setup admits a computer to the fleet before anyone signs in, so the two blast radii are not
--   the same and a later loosening of one must not open the other (research D11).
--
--   Written as its own statements rather than woven into the nine role blocks above, for the same reason
--   `permission.settings.storage_paths` is: what each role inherited from the retired lists stays unedited and
--   this addition is reviewable on its own.
-- ---------------------------------------------------------------------------
INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.machine_configuration', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.ignored_locations_edit', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.log_panel', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:it_department', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.log_panel', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.session_length', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:plant_manager', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.session_length', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- ---------------------------------------------------------------------------
-- 013-lugger-location-config (task T006) — permission.settings.lugger_arrangement
--
--   The arrangement is the one settings surface a lead edits from the floor, so the six rungs from Material
--   Handler Lead upward answer 1 and the three worker roles below that rung answer 0 (FR-028). Reading a
--   lugger on a card is not a permission: the cell follows the request, not the reader. A role with no row
--   here is refused, which is what a missing row means everywhere else in this file.
--
--   Written as its own statement rather than woven into the nine role blocks above, for the same reason
--   `permission.settings.storage_paths` is: what each role inherited from the retired lists stays unedited and
--   this addition is reviewable on its own.
-- ---------------------------------------------------------------------------
INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:production_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:setup_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:material_handler_lead', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.lugger_arrangement', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- Buildings catalog — permission.settings.buildings. Same nine-row shape as the other stated keys: Plant
-- Manager, IT Department and Developer answer 1 and every other role answers 0.
INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value_bool,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'permission.settings.buildings', 'role', 'role:developer', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:it_department', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:plant_manager', 1, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:production_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:setup_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:material_handler_lead', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:material_handler', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:production', 0, 'bool', NULL, UTC_TIMESTAMP()),
    (UUID(), 'permission.settings.buildings', 'role', 'role:setup', 0, 'bool', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_bool = VALUES(setting_value_bool),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- The two plant-scoped preference rows this seed owns (010-startup-rebuild T053, T054).
--
-- They are here rather than only in `seed_permission_role_baselines/create.sql` because THIS file is what the
-- installer runs: `AllSeeds.sql` is the hand-maintained aggregate, and a row that exists only in the individual
-- seed is a row a fresh store never receives. That is exactly what happened on 2026-09-27: the validator
-- reported `Feature.IgnoredLocations` and `auth.session_length_minutes` missing at scope all_users after an
-- install that had run successfully, because the aggregate had never carried them.
--
-- The plant's list of locations to hide (FR-024). The value is the normalized, uppercase, comma-separated form
-- `IgnoredLocationsService` returns, so the store holds the same shape the code compares against rather than a
-- second shape that has to be parsed back.
INSERT INTO
    config_settings_values (
        public_id,
        setting_key,
        scope_type,
        scope_key,
        setting_value,
        value_type,
        updated_by_user_id,
        updated_utc
    )
VALUES
    (UUID(), 'Feature.IgnoredLocations', 'all_users', 'all_users', 'WC,NCM,V-WC,NCM-VITS,SHIP', 'text', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value = VALUES(setting_value),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- The session length (FR-029, research D17): eight hours, held against the plant and read when a session row is
-- written. Minutes, because that is the unit the launch resolves and the settings panel writes
-- (`ScopedPreferenceKeys.SessionLengthMinutes`).
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
VALUES
    (UUID(), 'auth.session_length_minutes', 'all_users', 'all_users', 480, 'int', NULL, UTC_TIMESTAMP())
ON DUPLICATE KEY UPDATE
    setting_value_int = VALUES(setting_value_int),
    value_type = VALUES(value_type),
    updated_utc = VALUES(updated_utc);

-- Seed: seed_role_admin_to_it_department
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T032)
-- Purpose: the administrator role is removed and IT Department takes its place, with every account moved across
--          (FR-011, FR-012, FR-013, FR-014).
--
-- Why this is a stored program and not a plain script
--   The change is three statements that must all land or none: insert the incoming role, repoint every
--   assignment, remove the outgoing row. MySQL 5.7 has no DECLARE ... HANDLER outside a stored program, so the
--   work lives in a procedure this script creates, calls and drops. The handler rolls back and resignals, so the
--   interrupted state FR-013 forbids, an account holding no role, cannot be left behind. The procedure is
--   transient on purpose: it is not a schema artifact, so it is dropped before this script ends and it appears
--   in no aggregate and no folder of its own.
--
-- The rung is written explicitly
--   `it_department` is inserted with role_rank = 90 and not left to the column default. The column is
--   NOT NULL DEFAULT 0 (T004), so an insert that omitted it would put the renamed top role BELOW the worker
--   roles at 10, and the rank rule would then refuse it the accounts it exists to own.
--
-- The order of the three statements
--   Insert, repoint, remove. `auth_roles_assignments.role_id` carries a RESTRICT foreign key back to the
--   catalogue, so the retired row cannot be removed while an assignment still names it.
--
-- The reversal, and the window in which it is exact
--   rollback.sql restores the row this migration removed and moves the accounts back. That reversal is exact
--   only while no account has been given IT Department since this migration ran. Without a record of which
--   accounts this script moved, the reversal moves every account standing on IT Department; inside that window
--   no other writer can have put one there, so the two sets are the same set. The retired row's `public_id`
--   cannot be restored either, because the row is gone: the reversal gives it a fresh one, which no reader
--   keys on. Both headers state this, because the window is the whole reason the reversal is exact.
--
-- Running it twice
--   A second run finds no retired row to remove and leaves the store alone. It reports that rather than failing,
--   so re-running the file is safe.

USE mtm_waitlist;

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS sp_seed_role_admin_to_it_department;

DELIMITER $$

CREATE PROCEDURE sp_seed_role_admin_to_it_department()
BEGIN
    DECLARE v_retired_role_id BIGINT DEFAULT NULL;
    DECLARE v_incoming_role_id BIGINT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;

        RESIGNAL;
    END;

    START TRANSACTION;

    SET v_retired_role_id = (
        SELECT r.id
        FROM auth_roles_catalog r
        WHERE r.role_code = 'admin'
        LIMIT 1
    );

    IF v_retired_role_id IS NOT NULL THEN
        SET v_incoming_role_id = (
            SELECT r.id
            FROM auth_roles_catalog r
            WHERE r.role_code = 'it_department'
            LIMIT 1
        );

        IF v_incoming_role_id IS NULL THEN
            INSERT INTO auth_roles_catalog (
                public_id,
                role_code,
                role_name,
                role_rank,
                created_utc,
                updated_utc
            )
            VALUES (
                UUID(),
                'it_department',
                'IT Department',
                90,
                UTC_TIMESTAMP(),
                UTC_TIMESTAMP()
            );

            SET v_incoming_role_id = LAST_INSERT_ID();
        ELSE
            UPDATE auth_roles_catalog
            SET role_name = 'IT Department',
                role_rank = 90,
                updated_utc = UTC_TIMESTAMP()
            WHERE id = v_incoming_role_id;
        END IF;

        UPDATE auth_roles_assignments
        SET role_id = v_incoming_role_id
        WHERE role_id = v_retired_role_id;

        DELETE FROM auth_roles_catalog
        WHERE id = v_retired_role_id;
    END IF;

    COMMIT;
END$$

DELIMITER ;

CALL sp_seed_role_admin_to_it_department();

DROP PROCEDURE IF EXISTS sp_seed_role_admin_to_it_department;

-- Reported, so a run that changed nothing is visible rather than inferred.
SELECT
    'admin absent, it_department present' AS expected_after_apply,
    (SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = 'admin') AS retired_rows,
    (SELECT COUNT(*) FROM auth_roles_catalog WHERE role_code = 'it_department') AS renamed_rows,
    (
        SELECT r.role_rank
        FROM auth_roles_catalog r
        WHERE r.role_code = 'it_department'
        LIMIT 1
    ) AS renamed_rung,
    (
        SELECT COUNT(*)
        FROM auth_roles_assignments ra
        INNER JOIN auth_roles_catalog r ON r.id = ra.role_id
        WHERE r.role_code = 'it_department'
    ) AS accounts_on_the_renamed_role,
    (
        SELECT COUNT(*)
        FROM core_users_profiles u
        WHERE u.username_normalized IS NOT NULL
          AND NOT EXISTS (
                SELECT 1
                FROM auth_roles_assignments ra
                WHERE ra.user_id = u.id
          )
    ) AS accounts_with_no_role;

-- Seed: seed_setup_work_centers_default
-- Engine: MySQL 5.7
-- Source: live Visual SHOP_RESOURCE work center list

USE mtm_waitlist;

SET FOREIGN_KEY_CHECKS = 0;

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
        '100-03',
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
        '100-06',
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
        '100-07',
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
        '100-08',
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
        '100-09',
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

-- Seed: seed_username_upper_normalization
-- Engine: MySQL 5.7
-- Feature: 006-user-management-and-permissions (task T015)
-- Purpose: bring every stored sign-in name onto the upper-case rule (FR-003, FR-002).
--
-- How it makes itself reversible
--   The original spelling is destroyed by the change, so it is recorded first, in
--   `seed_username_case_backup`. That table exists only for this migration's lifetime: rollback.sql reads it,
--   restores what it holds, and drops it. It is not a permanent schema artifact, which is why it is not listed
--   in AllTables.sql and has no create.sql of its own.
--   Re-running create.sql cannot overwrite a recorded original: the backup insert keeps the value it already
--   holds, so the first recording of a row is the one that survives.
--
-- How it reports rather than fails
--   A row whose upper-cased name would collide with another row's upper-cased name is left alone, and every
--   such row is listed in the result set at the end of this script. Nothing here aborts: a reported row is a
--   thing for a DBA to look at, not a reason to stop the migration.
--
-- A finding worth knowing, recorded here because only implementation revealed it
--   Every column here is `utf8mb4_unicode_ci`, which is case-INSENSITIVE, and that cuts both ways:
--   * The comparison must be forced binary. `u.username_normalized <> UPPER(u.username_normalized)` is FALSE
--     under that collation, because the two sides compare equal; the naive form therefore selects nothing and
--     the migration silently does nothing. Every comparison below is written `BINARY A <> BINARY B` for that
--     reason. This was caught by running the seed against a real store, not by reading it.
--   * `username_normalized` also carries a UNIQUE key under the same collation, so two rows differing only in
--     case cannot both exist. The collision branch is therefore not reachable through that key as the schema
--     stands; it can only fire if a store has been given a different collation. It is kept because the rule it
--     protects is real and a collation is not something this seed can assume.
--   The collation is also why FR-002's "signing in succeeds whatever case is typed" already held before this
--   change: the comparison folded case. What this migration actually fixes is the STORED form, so the column is
--   consistent for readers that compare it exactly and for anybody reading the table.

USE mtm_waitlist;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS seed_username_case_backup (
    user_id BIGINT NOT NULL,
    previous_username_normalized VARCHAR(128) NOT NULL,
    recorded_utc DATETIME NOT NULL,
    PRIMARY KEY (user_id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

INSERT INTO seed_username_case_backup (
    user_id,
    previous_username_normalized,
    recorded_utc
)
SELECT u.id, u.username_normalized, UTC_TIMESTAMP()
FROM core_users_profiles u
WHERE BINARY u.username_normalized <> BINARY UPPER(u.username_normalized)
  AND NOT EXISTS (
        SELECT 1
        FROM core_users_profiles x
        WHERE x.id <> u.id
          AND BINARY UPPER(x.username_normalized) = BINARY UPPER(u.username_normalized)
  )
ON DUPLICATE KEY UPDATE
    previous_username_normalized = seed_username_case_backup.previous_username_normalized;

SELECT 'skipped: would collide on the unique key' AS report, u.id AS user_id, u.username_normalized
FROM core_users_profiles u
WHERE BINARY u.username_normalized <> BINARY UPPER(u.username_normalized)
  AND EXISTS (
        SELECT 1
        FROM core_users_profiles x
        WHERE x.id <> u.id
          AND BINARY UPPER(x.username_normalized) = BINARY UPPER(u.username_normalized)
  )
ORDER BY u.username_normalized ASC;

UPDATE core_users_profiles u
INNER JOIN seed_username_case_backup b ON b.user_id = u.id
SET u.username_normalized = UPPER(u.username_normalized),
    u.updated_utc = UTC_TIMESTAMP();

-- Seed: seed_waitlist_request_item_configs
-- Engine: MySQL 5.7
-- Purpose: Populate waitlist_request_item_configs with one row for every one of the twenty-four catalogued
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
--     enumerated values arrive with the job snapshot rather than from configuration.
--   * deliver-component is pickup-component's Deliver counterpart (added 2026-09-22, so a job with a component
--     is offered a Deliver component wherever Pickup offers one). It names the `component` job list explicitly,
--     which resolves to the same job list pickup-component reaches by fallback, so the two rows cannot drift.
--   * pickup-die and deliver-die ask *which* die the request is for, so they too carry no options_json: the
--     choices are the dies the requesting job records, and the `list` key on the answer field names that job
--     list so the choice never depends on the Item's identity (FR-054, D22). The retired Take To question is
--     gone with it: a die always goes to its home location, so nothing asks or stores where it is going.
--   * pickup-dunnage and deliver-dunnage ask *which* dunnage the operator needs, so they too carry no
--     options_json: the choices are the dunnage parts assigned to the requesting job, and the `list` key on the
--     answer field names that job list so the choice never depends on the Item's identity (FR-013, FR-048).
--   * detail_fields_json's key order stays inside the JSON payload: a bare `order` column is banned by
--     Database/Database-Ruleset.md and is never introduced.
--   * Seed reconciliation (2026-10-10, spec 012 §9, D33): four declared labels are corrected in place to the
--     labels the card reads — `Pickup location` to `Die Location` on both die Items, `Home location` to
--     `Home Location` on both dunnage Items, `Scrap type` to `Scrap Type`, and `Coil number` to `Coil Number`
--     on assist-coil-turn only (the coil Items keep their own `Coil number`). The corrected labels are appended
--     after every field the Item already declared, so no existing declaration is reordered. An installed store
--     is moved without a reinstall by seed_waitlist_request_item_configs_label_move, whose rollback restores the
--     previous spellings.
--   * Seed reconciliation (2026-10-10, spec 012 §9): the cells the card drew and configuration did not carry
--     are appended as declarations — `Die Shop Direction` on both die Items, `Size` on both hopper Items,
--     `# of Skids` on the four returning pickups, `Weight of Coil` on assist-coil-turn, `Part Type` on the four
--     returning pickups, `Item Type` and `Total Qty In House` on both component Items, and `Lugger Location` on
--     pickup-scrap. Every one of them is a label the card's own rules already read; none of them is invented
--     here.
--   * Nothing is removed: every declared field the card does not draw is left declared and excluded by
--     WaitlistCardCellRules instead (FR-056), so the exclusion stays a card rule rather than a configuration
--     edit. `Description` on the two dunnage Items is the live example — it is provably unfillable, and whether
--     to remove it is an open question rather than a silent deletion (FR-095).
--   * Only the two wrong-part Items change a field's source: their incorrect part (`Coil number`,
--     `Part`) moves from `job` to `answer`, because the operator types it through the fuzzy search (FR-082).
--     Their correct part stays the job's own (FR-088).

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
 'Which die is this request for?', 0, 200,
 NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Die','value_type','enum','source','answer','list','die','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Die Location','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Die Shop Direction','value_type','string','source','answer','order',4,'is_required',FALSE)),
 45, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000003', 'pickup-component', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Choose the component to collect.', 0, 200,
 NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Component','value_type','enum','source','answer','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Total Qty In House','value_type','string','source','job','order',5,'is_required',FALSE),
   JSON_OBJECT('label','Item Type','value_type','string','source','job','order',6,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000004', 'pickup-fg', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity remaining','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Customer','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Packlist','value_type','string','source','job','order',5,'is_required',FALSE),
   JSON_OBJECT('label','Part Type','value_type','string','source','job','order',6,'is_required',FALSE),
   JSON_OBJECT('label','# of Skids','value_type','string','source','answer','order',7,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000005', 'pickup-ncm', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity to move','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Traceability ID','value_type','string','source','job','order',5,'is_required',FALSE),
   JSON_OBJECT('label','Part Type','value_type','string','source','job','order',6,'is_required',FALSE),
   JSON_OBJECT('label','# of Skids','value_type','string','source','answer','order',7,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000006', 'pickup-wip', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',2,'is_required',FALSE),
   JSON_OBJECT('label','WIP destination','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Operation sequence','value_type','string','source','job','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Part Type','value_type','string','source','job','order',5,'is_required',FALSE),
   JSON_OBJECT('label','# of Skids','value_type','string','source','answer','order',6,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000007', 'pickup-outside-service', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity to move','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Pickup location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Traceability ID','value_type','string','source','job','order',5,'is_required',FALSE),
   JSON_OBJECT('label','Part Type','value_type','string','source','job','order',6,'is_required',FALSE),
   JSON_OBJECT('label','# of Skids','value_type','string','source','answer','order',7,'is_required',FALSE)),
 NULL, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000008', 'pickup-riser-table', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000009', 'pickup-dunnage', 'Pickup', 'collect-input-then-confirm', 1, 'enum',
 'Which dunnage do you need from the material handlers?', 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Dunnage part','value_type','enum','source','answer','list','dunnage','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Home Location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Quantity on hand','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000010', 'pickup-scrap', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Scrap Type','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity involved','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Scrap lugger','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Lugger Location','value_type','string','source','job','order',5,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000011', 'pickup-hopper', 'Pickup', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Pickup work center','value_type','string','source','fixed','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Size','value_type','enum','source','answer','order',2,'is_required',TRUE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Deliver (9)
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
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Size','value_type','enum','source','answer','order',2,'is_required',TRUE)),
 20, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000015', 'deliver-flatstock', 'Deliver', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Work center','value_type','string','source','fixed','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Destination','value_type','string','source','fixed','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000024', 'deliver-component', 'Deliver', 'collect-input-then-confirm', 1, 'enum',
 'Choose the component to bring.', 0, 200,
 NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Component','value_type','enum','source','answer','list','component','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Part description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Requesting work center','value_type','string','source','fixed','order',4,'is_required',FALSE),
   JSON_OBJECT('label','Total Qty In House','value_type','string','source','job','order',5,'is_required',FALSE),
   JSON_OBJECT('label','Item Type','value_type','string','source','job','order',6,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000016', 'deliver-die', 'Deliver', 'collect-input-then-confirm', 1, 'enum',
 'Which die is this request for?', 0, 200,
 NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Die','value_type','enum','source','answer','list','die','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Die Location','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Quantity','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Die Shop Direction','value_type','string','source','answer','order',4,'is_required',FALSE)),
 45, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000017', 'deliver-dunnage', 'Deliver', 'collect-input-then-confirm', 1, 'enum',
 'Which dunnage do you need from the material handlers?', 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Dunnage part','value_type','enum','source','answer','list','dunnage','order',1,'is_required',TRUE),
   JSON_OBJECT('label','Description','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Home Location','value_type','string','source','job','order',3,'is_required',FALSE),
   JSON_OBJECT('label','Quantity on hand','value_type','string','source','job','order',4,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000018', 'deliver-wrong-coil', 'Deliver', 'collect-input-then-confirm', 1, 'text',
 'Explain how or why the coil is wrong', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil number','value_type','string','source','answer','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Reason','value_type','text','source','answer','order',2,'is_required',TRUE),
   JSON_OBJECT('label','Correct coil','value_type','string','source','job','order',3,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

('c1000000-0000-4000-8000-000000000019', 'deliver-wrong-flatstock', 'Deliver', 'collect-input-then-confirm', 1, 'text',
 'Explain how or why the flatstock is wrong', 5, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Part','value_type','string','source','answer','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Reason','value_type','text','source','answer','order',2,'is_required',TRUE),
   JSON_OBJECT('label','Correct flatstock','value_type','string','source','job','order',3,'is_required',FALSE)),
 30, UTC_TIMESTAMP(), UTC_TIMESTAMP()),

-- ------------------------------------------------------------------ Assist (3)
('c1000000-0000-4000-8000-000000000020', 'assist-coil-turn', 'Assist', 'direct-to-confirmation', 0, NULL, NULL,
 0, 200, NULL,
 JSON_ARRAY(
   JSON_OBJECT('label','Coil Number','value_type','string','source','job','order',1,'is_required',FALSE),
   JSON_OBJECT('label','Quantity in house','value_type','string','source','job','order',2,'is_required',FALSE),
   JSON_OBJECT('label','Weight of Coil','value_type','string','source','answer','order',3,'is_required',TRUE)),
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

-- Seed: seed_waitlist_request_item_configs_label_move
-- Engine: MySQL 5.7
-- Purpose: Move the four declaration labels the card reads onto the spellings it reads them under, in place, so
--          an installed store gets `Die Location`, `Home Location`, `Scrap Type` and `Coil Number` without the
--          truncate-and-reinsert of Database/Seeds/seed_waitlist_request_item_configs wiping operator edits
--          (D33, FR-059).
-- Scope:   1. `Pickup location` to `Die Location` on pickup-die and deliver-die.
--          2. `Home location` to `Home Location` on pickup-dunnage and deliver-dunnage.
--          3. `Scrap type` to `Scrap Type` on pickup-scrap.
--          4. `Coil number` to `Coil Number` on assist-coil-turn only. The coil Items keep `Coil number`, which
--             is the label their own cell is declared and read under, so this move is scoped to the one Item
--             whose cell is spelled `Coil Number` (data-model.md §9).
--          5. `Part number` to `Part` on pickup-fg, and 6. `Part and quantity` to `Part` on pickup-wip, so the
--             four returning pickups all draw the part the work centre is making under one label.
-- Method:  Each statement rewrites one label inside detail_fields_json through JSON_SEARCH, so the path is the
--          path the label actually sits at rather than a guessed array index: a store whose fields were added,
--          removed or reordered still moves the label it holds, and a store that does not hold it is left
--          untouched. Every other key of that field, every other field, and every other column of the row are
--          left exactly as they were; only the label and the row's updated_utc change.
-- Rerunnable: The guard is the presence of the label being moved, so a second run matches no row and changes
--          nothing. The statements are the only writes, and there is no TRUNCATE anywhere in this file.
-- Not in scope: The cells this feature adds (`Die Shop Direction`, `Size`, `# of Skids`, `Weight of Coil`,
--          `Part Type`, `Item Type`, `Total Qty In House`, `Lugger Location`) and the two source corrections on
--          the wrong-part Items are declarations rather than labels, and they arrive with
--          Database/Seeds/seed_waitlist_request_item_configs, which the installer applies. This file moves
--          labels only, so a store that is updated by hand never has its field set rewritten wholesale.

USE mtm_waitlist;

SET NAMES utf8mb4;

-- 1. The die Items' location label.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Pickup location', NULL, '$[*].label')),
        'Die Location'),
    updated_utc = UTC_TIMESTAMP()
WHERE item IN ('pickup-die', 'deliver-die')
  AND JSON_SEARCH(detail_fields_json, 'one', 'Pickup location', NULL, '$[*].label') IS NOT NULL;

-- 2. The dunnage Items' home-location label.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Home location', NULL, '$[*].label')),
        'Home Location'),
    updated_utc = UTC_TIMESTAMP()
WHERE item IN ('pickup-dunnage', 'deliver-dunnage')
  AND JSON_SEARCH(detail_fields_json, 'one', 'Home location', NULL, '$[*].label') IS NOT NULL;

-- 3. The scrap Item's type label.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Scrap type', NULL, '$[*].label')),
        'Scrap Type'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-scrap'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Scrap type', NULL, '$[*].label') IS NOT NULL;

-- 4. The assist coil turn's coil label. Scoped to that one Item: the coil Items' own cell is `Coil number`.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Coil number', NULL, '$[*].label')),
        'Coil Number'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'assist-coil-turn'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Coil number', NULL, '$[*].label') IS NOT NULL;

-- 5. The finished-goods pickup's part label: the card reads the part the work centre is making as `Part`.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Part number', NULL, '$[*].label')),
        'Part'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-fg'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Part number', NULL, '$[*].label') IS NOT NULL;

-- 6. The work-in-process pickup's part label, for the same reason.
UPDATE waitlist_request_item_configs
SET detail_fields_json = JSON_REPLACE(
        detail_fields_json,
        JSON_UNQUOTE(JSON_SEARCH(detail_fields_json, 'one', 'Part and quantity', NULL, '$[*].label')),
        'Part'),
    updated_utc = UTC_TIMESTAMP()
WHERE item = 'pickup-wip'
  AND JSON_SEARCH(detail_fields_json, 'one', 'Part and quantity', NULL, '$[*].label') IS NOT NULL;

-- Seed: seed_waitlist_request_item_images
-- Engine: MySQL 5.7
-- Purpose: retire the item pictures this seed used to write.
--
-- What it used to do, and why it cannot do it any more:
--   It seeded an Item's own picture for six Items and a Category family picture for three Categories, every
--   one of them pointing at `Assets/RequestTypes/*.png`. Those files were 68-byte single-pixel stand-ins for
--   artwork that was never delivered, and the folder is gone from the repository altogether. Every one of the
--   rows therefore resolved to the "no image" placeholder, so what the store held was a claim with nothing
--   behind it: the picture screen showed a custom image that was not there, and the row's only real effect was
--   to say "this Item has a picture" when it had none.
--
-- What is seeded now: nothing.
--   An Item's picture is a setting an operator makes through the picture screen, and the application's own
--   answer to "nothing configured" is the one no-image placeholder. A seeded row could only ever duplicate
--   that answer while pretending to be a picture, which is the lie this seed's own header used to warn about.
--
-- What the delete is for:
--   A store that ran an earlier version of this seed still holds those rows. They are removed by the one thing
--   that identifies them — the folder that never shipped — so an operator's own choice under either scope is
--   left exactly where it is, and re-running is both idempotent and non-destructive. (The earlier version
--   cleared both scopes outright before re-inserting; that is no longer true and no longer wanted.)
--
-- Reversal: see rollback.sql. There is nothing to put back.

USE mtm_waitlist;

SET NAMES utf8mb4;

SET FOREIGN_KEY_CHECKS = 0;

DELETE FROM config_images_locations WHERE image_path LIKE 'Assets/RequestTypes/%';

SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------------------------------
-- Seed: seed_retire_computer_scope_picture_sources
-- ---------------------------------------------------------------------------
-- Feature: 010-startup-rebuild (task T198)
--
-- Withdraws every `computer` scope row in `config_images_locations`, so a computer set up before this change
-- reads its pictures from the plant-wide setting afterwards (FR-040, SC-021). The three per-computer sources
-- were written by `sp_config_images_locations_computer_sources_set`, which this task retires; what an existing
-- computer still holds is three rows of its own, and withdrawing them is what leaves the plant-wide answer as the
-- only one in force. It withdraws rather than deletes, so the change history keeps reading what was there and a
-- store that needs one row back can reactivate it by its public id.
--
-- It depends on nothing but the procedures being installed, which happens before every seed, and no other seed
-- reads or writes a `computer` scope row, so its position here is free.
-- ---------------------------------------------------------------------------

USE mtm_waitlist;

CALL sp_config_images_locations_deactivate_for_scope('computer', NULL);

-- ---------------------------------------------------------------------------
-- Seed: seed_waitlist_lugger_arrangement_defaults
-- ---------------------------------------------------------------------------
-- Feature: 013-lugger-location-config (task T025)
--
-- Placed last on purpose. It writes `waitlist_lugger_locations` and `waitlist_lugger_scrap_assignments`, so
-- the three lugger tables must already exist; every table in `Database/Tables/AllTables.sql` is applied
-- before the seed aggregate is read, which is the only ordering this seed needs. It reads no other seed's
-- rows and no other seed reads its rows, so no seed that follows it can be broken by its position here and
-- none that precedes it has to run after it. On a store that already holds luggers the seed writes nothing.
--
-- The artifact's own header and statements follow verbatim from
-- `Database/Seeds/seed_waitlist_lugger_arrangement_defaults/create.sql`, which stays the source of record.
-- ---------------------------------------------------------------------------
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
