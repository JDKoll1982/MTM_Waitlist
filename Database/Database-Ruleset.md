# MTM_Waitlist Database Ruleset

## Decision Lock
This document applies your completed naming conventions and database architecture decisions as hard requirements for MySQL 5.7 schema work.

## Engine and Environment
- MySQL 5.7.
- utf8mb4 / utf8mb4_unicode_ci.
- Separate dev/test/prod databases.
- Production identity/session data stays production-only.
- Non-prod must use masked/synthetic data.

## Naming Standards (Locked)
- Table names: plural, lowercase snake_case, structured module pattern:
- `{category}_{table}_{action_or_purpose}`
- Primary key column: `id`.
- Public UUID column: `public_id`.
- FK column format: relationship descriptor + `_id` (for example, `user_id`, `workstation_id`).
- UTC datetime suffix: `_utc`.
- Boolean prefix: `is_` or `has_`.

## Constraint and Index Naming
- PK naming: MySQL default (`PRIMARY`).
- FK naming: `fk_<from_table>_<to_table>_<column>`.
- Unique naming: `uq_<table>_<column_or_purpose>`.
- Index naming: `idx_<table>_<column_or_purpose>`.
- Composite index ordering: left-to-right by filter/selectivity.
- MySQL identifier max length: 64 characters (hard limit for key/constraint/index names).
- If a generated constraint/index name exceeds 64 characters, shorten descriptive middle segments while preserving required prefixes and meaning.

## Allowed and Banned Terms
- Allowed abbreviations: `id`, `utc`, `mac`, `ip`, `rbac`, `uuid`.
- Banned abbreviations: `cfg`, `usr`, `ws`, `sess`, `rpt`.
- Banned words: `class`, `delete`, `order`.
- Approved exceptions:
  - `value_type` is explicitly accepted.
  - `order` is allowed where it forms the domain compound `work_order` (for example `work_order`,
    `normalized_work_order`, `open_work_order_quantity`), because that is established Infor Visual
    vocabulary. A bare `order` identifier is still banned.

## Core Startup Tables
- `core_users_profiles`
- `core_computers_registry`
- `auth_roles_catalog`
- `auth_roles_assignments`
- `auth_sessions_tokens`
- `ops_startup_logs`
- `config_settings_values`
- `config_settings_history`

## Session and Security
- Session tokens: salted hash + metadata only, never plaintext.
- Session validity checked against DB UTC source via function call.
- Workstation identity relies on normalized hostname + MAC fields.

## Data Lifecycle and Auditing
- Hard delete policy for primary records.
- Use `_history` tables for change auditability.
- Retention windows are dynamic settings values from `config_settings_values`.

## Settings Scope and Resolution
- Settings are persisted in `config_settings_values`; changes are recorded in `config_settings_history`.
- Supported scope types are `workstation`, `all_users`, `role`, `user`, `admin`, and `developer`.
- Scope resolution is ordered from fallback to override:
  `workstation` -> `all_users` -> `role` -> `admin` -> `developer` -> `user`.
- **The order is the corrected one, re-ranked by 006-user-management-and-permissions (T006).** The `role` scope is
  new, and `user` moved from third to last so that **a person's own value wins**: before the re-rank an `admin` or
  `developer` row outranked the value chosen for the person themselves, and the person's own choice was silently
  overridden with nothing reporting it. `fn_config_settings_scope_rank` is the one place the numbers live, and the
  effective read keeps taking the highest-ranked applicable row.
- The re-rank ships **before** the first per-person permission row is ever written (T054), because a row written
  under the old order would read as if it had no effect. That ordering is a gate rather than advice.
- `scope_key` is required and is `workstation:<id>`, `all_users`, `role:<role_code>`, `user:<id>`, `admin`, or
  `developer`.
- A **role baseline** is one `role`-scoped row per permission per role, keyed `role:<role_code>` with
  `value_type` `bool`. It is shipped as data by `Seeds/seed_permission_role_baselines`, so what a role may do is
  changed without a code change. A **person permission value** is a `user`-scoped row in the same table, written
  only when the person's answer differs from their role's baseline, and it is written through
  `sp_config_permissions_user_set` rather than through `sp_config_settings_upsert`.
- Workstation and user scopes use `workstation_id` and `user_id` foreign keys respectively.
- Admin and developer scope writes require role authorization; raw secrets must remain outside the settings table.
- Effective reads must use `sp_config_settings_get_effective`; writes must use `sp_config_settings_upsert`, except
  the permission writes named above, which have their own procedures and their own history record.
- **A value row cannot be deleted and still be recorded.** `config_settings_history.config_setting_id` is a foreign
  key to `config_settings_values.id`, so a removal cannot write a history row naming the row it removed. A reversal
  therefore restores the *value* that was in force rather than the absence of a row.

## Part Picture Storage (008-part-pictures)

Every part the application can name may carry a picture of its own, kept apart by system so a Visual part and a WIP
part that share a number are two pictures. The store already held what this needed; what follows is what the feature
added or changed inside it.

- **The five `config_images_locations.scope` values** are `request_item`, `request_category`, `work_center`,
  `visual_part` and `wip_part`. The column and the unique key `uq_config_images_locations_scope_item` are unchanged:
  a part picture is one row per scope-and-item, exactly as a request-item picture always was. A part is identified by
  its system and its number together, never by the part number alone, so the two part scopes are two rows.
- **`config_images_locations_history` is new** and is the change record FR-027 asks for: nine columns, a
  `uq_config_images_locations_history_public_id` unique key, an `idx_config_images_locations_history_scope_item`
  index, and two `ON DELETE SET NULL` foreign keys so a record outlives the row it describes. A row is written in the
  same transaction as the write it records — a first picture carries a null predecessor, a replacement carries the
  path it replaced — by `sp_config_images_locations_insert` and `sp_config_images_locations_update`, and it is read
  through `sp_config_images_locations_history_get`. No application code writes it.
- **The layout inside the configured root** is compiled, while the root itself stays a setting. Every picture sits
  inside a collection folder: `Waitlist` (everything the application pictures for its own screens, one folder per
  kind inside it), `Visual` and `WIP` (the two part systems). Inside a part collection a picture is kept in the
  folder its part number's prefix decides — `MMC`, `MMF`, `FGT` — with `Categorized Parts` as the catch-all for a
  number no prefix recognises, and the file is named after the part. What makes a recorded value portable is that it
  is stored **relative** to the root, not that these folders are configurable: a value is read against whichever
  root the machine is configured with, so a picture recorded under a drive letter resolves under a UNC path.
- **The three kinds inside the application's own collection have their own folders**, named for their scopes
  verbatim. That is what settles the case-only collision between the item `other` and the category `Other`: neither
  name is changed, and neither can resolve to the other's file (FR-038).
- **The move of everything already stored** is `Seeds/seed_picture_layout_move` with
  `sp_config_images_locations_paths_move`, which rewrites each recorded path and reports through affected rows, with
  its own paired `rollback.sql`; the files themselves are moved by `tools/Move-PartPictureLayout.ps1`. It is
  run-once, and it is safe to interrupt: `AppStoragePaths.ToCurrentLayout` reads a value in either layout, so a
  picture resolves in the gap between the files moving and the rows moving. `AllTables.sql`, `AllSPs.sql` and
  `AllSeeds.sql` are regenerated in the same change, and the schema is proved by
  `Database/Validation/part_pictures_schema/validate.sql`.
- **A replaced picture is kept, then cleaned up.** The storage service copies the picture it replaces into an
  `Archive` folder beside it, and the startup cache step removes an archived file once
  `image_storage.archive_keep_days` days have passed. The period is a stored setting owned by the storage screen and
  defaults to ninety days (FR-037); it is not compiled into the application.
- **The picture entitlement was widened, the storage entitlement was not.** `permission.settings.part_pictures`
  becomes 1 for `role:setup_lead` and `role:plant_manager` as well as the IT Department and Developer, while
  `permission.settings.storage_paths` keeps its members: the folders stay with IT Department and Developer (FR-017,
  FR-025). Both live in `Seeds/seed_permission_role_baselines`, whose rollback restores exactly the two changed rows.

## Artifact Layout and Release Governance
- Use a file-per-artifact layout under `Database/`.
- Bootstrap database creation lives in `Database/Bootstrap/create_database.sql`.
- Table and column description updates live in `Database/Bootstrap/update_table_descriptions.sql`.
- Updating `Database/Bootstrap/update_table_descriptions.sql` is mandatory whenever any SQL artifact is added, modified, or removed under `Database/`.
- Table artifacts live in `Database/Tables/<table_name>/create.sql` and `rollback.sql`.
- Stored procedure artifacts live in `Database/StoredProcedures/<procedure_name>/create.sql` and `rollback.sql`.
- View artifacts live in `Database/Views/<view_name>/create.sql` and `rollback.sql`.
- Seed artifacts live in `Database/Seeds/<seed_name>/create.sql` and `rollback.sql`.
- Validation artifacts live in `Database/Validation/<validation_name>/validate.sql`.
- The reviewed SQL artifacts are the source of truth and are run as files; there is no FluentMigrator runner
  and no migration-runner layer in this repository.
- Production rollout: manual DBA-reviewed promotion with rollback prepared.
- Naming drift in SQL must fail CI/PR checks.
- Exceptions require explicit written approval in PR notes.

### Review note — rollback-only artifact folders (approved 2026-09-20)

Some folders under `Database/Tables/` hold **only** a `rollback.sql` and no `create.sql`. That is by design, not a
truncated artifact, and it must not be "tidied" away:

- `Database/Tables/20_mock_master_tables_registry` through `27_mock_request_types` are the retained drops for the
  retired in-database `mock_*` tables; `Database/Tables/28_waitlist_request_types` and
  `29_waitlist_request_subtypes` are the same pattern for the retired request-type catalog. The objects are gone
  from the schema, and the only remaining obligation is that an environment promoted from an older build still has
  a reviewed statement that removes them.
- These folders are deliberately exempt from the retired-symbol audit — `rollback.sql` **is** the drop statement for
  a retired object, so naming a removed object there is the record *of* the removal, not a stale reference
  (`MTM_Waitlist.Tests/Module_Mock/RetiredSymbolAuditTests.cs`, `IsExemptFromSqlAudit`). The matching instances are
  listed under the "Retired objects" sections of `Database/Bootstrap/update_table_descriptions.sql`.
- A rollback-only folder creates nothing, so it is **not** a duplicate of any create artifact and is outside the
  duplicate-object check `validate-database-schema.ps1` performs over `create.sql` files.
- The same convention applies to retired procedures under `Database/StoredProcedures/`.

### Review note — the `mtm_mock` mirror schema lives only under `Database/Mock/`

- The five `visual_*_result` mirror tables, their `_stage` twins and the `sp_visual_*` procedures are defined
  **once**, under `Database/Mock/Tables`, `Database/Mock/StoredProcedures` and `Database/Mock/Seeds`.
- `Database/Tables/AllTables.sql` does **not** list any `mtm_mock` object; the mirror ships through the generated
  `Database/Mock/All*.sql` masters.
- The `2x_mock_*` folders under `Database/Tables/` are the rollback-only retired-object drops above and create
  nothing, so no mirror object is defined twice. The folder names invite the opposite conclusion; this note is the
  record that the opposite conclusion is wrong.

### Review note — generated master lists for the `mtm_mock` cache (approved 2026-09-11)

The rule above that a master list is "hand-maintained" does not fit the `mtm_mock` cache, so the deviation is
recorded here rather than left implicit (task T121 of `specs/001-module-mock-visual-fallback`):

- **Deviation.** `Database/Mock/AllTables.sql`, `Database/Mock/AllSPs.sql` and `Database/Mock/AllSeeds.sql`
  are **generated**, not hand-maintained, by `Database/CopilotScripts/build_mtm_mock_masters.ps1`, which
  concatenates the file-per-artifact `create.sql` sources in dependency order.
- **Why.** The cache ships 5 mirror tables, 5 stage twins and 10 procedures through the same file-per-artifact
  convention the rest of `Database/` uses. Hand-copying those bodies into a second place is exactly the drift
  the naming rule exists to prevent, and it is unreviewable in a diff: a change to one artifact would have to
  be mirrored by hand in an aggregate no one reads line by line.
- **Why it is safe.** The generator is not a build-time code path — the master lists are committed files and a
  DBA reviews them exactly as before. `-Check` regenerates in memory and exits non-zero when a committed list
  is stale, which is the staleness gate `verify_mtm_mock_deploy.ps1` runs, so a hand-edited or forgotten list
  fails instead of shipping.
- **Scope.** `mtm_mock` only. The aggregates for the internal stores
  (`Database/Tables/AllTables.sql`, `Database/StoredProcedures/AllSPs.sql`, `Database/Seeds/AllSeeds.sql`) stay
  hand-maintained and are edited directly.
- **Still mandatory, unchanged.** Every artifact continues to ship `create.sql` + `rollback.sql` in the same
  change and to be registered in `Database/Mock/Bootstrap/update_table_descriptions.sql`.

## Waitlist Request Persistence Guidance
- For waitlist request persistence work, prefer the existing MySQL stored-procedure pattern and current operational tables before introducing new schema objects.
- Reuse the established helper-server execution style (`MySqlHelperServer` + parameterized stored procedures / SQL) instead of creating ad hoc direct SQL wiring in each feature service.
- A new table is allowed only when the workflow clearly requires a distinct domain model that cannot be represented by the current operational data model without breaking invariants.
- All new or modified SQL artifacts must still follow the file-per-artifact convention and must update `Database/Bootstrap/update_table_descriptions.sql` when applicable.
- Blockpoint: do not duplicate persistence logic across feature services; centralize the request creation contract through the helper-server pattern and make any new stored procedure contract explicit in the repo documentation before implementation.
