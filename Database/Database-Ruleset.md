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

## Migration and Release Governance
- Use `0001__description.sql` naming.
- Use `0001__description_rollback.sql` naming.
- Migration model: Hybrid EF + DBA-reviewed SQL.
- Production rollout: manual DBA-reviewed promotion with rollback prepared.
- Naming drift in SQL must fail CI/PR checks.
- Exceptions require explicit written approval in PR notes.
