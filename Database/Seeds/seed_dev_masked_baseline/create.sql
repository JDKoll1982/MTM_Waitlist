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
--
-- This seed seeds no computers and no waitlist requests. A freshly built store therefore holds no configured
-- workstation and no request cards, so the first launch runs the whole startup workflow — machine setup, then
-- sign-in — and the waitlist board starts empty. A computer enters the registry when its own setup completes,
-- and a request enters the queue when somebody raises one.

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