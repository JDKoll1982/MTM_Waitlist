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
- Approved exception: `value_type` (explicitly accepted despite `type` in banned list).

## Core Startup Tables
- `core_users_profiles`
- `core_workstations_registry`
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
- Supported scope types are `workstation`, `all_users`, `user`, `admin`, and `developer`.
- Scope resolution is ordered from fallback to override: workstation -> all_users -> user -> admin -> developer.
- `scope_key` is required and is `workstation:<id>`, `all_users`, `user:<id>`, `admin`, or `developer`.
- Workstation and user scopes use `workstation_id` and `user_id` foreign keys respectively.
- Admin and developer scope writes require role authorization; raw secrets must remain outside the settings table.
- Effective reads must use `sp_config_settings_get_effective`; writes must use `sp_config_settings_upsert`.

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
- FluentMigrator is the recommended runner layer for executing the reviewed SQL artifacts.
- Production rollout: manual DBA-reviewed promotion with rollback prepared.
- Naming drift in SQL must fail CI/PR checks.
- Exceptions require explicit written approval in PR notes.

## Waitlist Request Persistence Guidance
- For waitlist request persistence work, prefer the existing MySQL stored-procedure pattern and current operational tables before introducing new schema objects.
- Reuse the established helper-server execution style (`MySqlHelperServer` + parameterized stored procedures / SQL) instead of creating ad hoc direct SQL wiring in each feature service.
- A new table is allowed only when the workflow clearly requires a distinct domain model that cannot be represented by the current operational data model without breaking invariants.
- All new or modified SQL artifacts must still follow the file-per-artifact convention and must update `Database/Bootstrap/update_table_descriptions.sql` when applicable.
- Blockpoint: do not duplicate persistence logic across feature services; centralize the request creation contract through the helper-server pattern and make any new stored procedure contract explicit in the repo documentation before implementation.
