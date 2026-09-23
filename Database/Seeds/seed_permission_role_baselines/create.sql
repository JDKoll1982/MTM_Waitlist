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
