---
applyTo: "Database/**/*.sql"
---

# MTM_Waitlist Database SQL Rules (Locked)

## Platform
- Engine: MySQL 5.7.
- Charset/collation defaults: utf8mb4 / utf8mb4_unicode_ci.
- Keep all SQL identifiers lowercase snake_case.
- Avoid quoted identifiers unless unavoidable.

## Environment Isolation
- Use physically separate databases for dev, test, and prod.
- Never move raw production user/session data to non-production.
- Dev/test seed data must be masked/synthetic.

## Naming Conventions (Hard Rules)
- Tables are plural and follow structured module prefixes:
- `{category}_{table}_{action_or_purpose}`
- Primary key column is always `id`.
- Public UUID column is always `public_id`.
- Foreign key columns use custom relationship descriptors (for example, `user_id`, `employee_id`, `workstation_id`).
- Boolean columns start with `is_` or `has_`.
- UTC datetime columns end with `_utc`.

## Constraint and Index Naming
- Primary key: use MySQL default `PRIMARY` naming.
- Foreign keys: `fk_<from_table>_<to_table>_<column>`.
- Unique constraints: `uq_<table>_<column_or_purpose>`.
- Non-unique indexes: `idx_<table>_<column_or_purpose>`.
- Composite index order: left-to-right by filter/selectivity.

## Reserved/Banned Terms and Abbreviations
- Allowed abbreviations: `id`, `utc`, `mac`, `ip`, `rbac`, `uuid`.
- Banned abbreviations: `cfg`, `usr`, `ws`, `sess`, `rpt`.
- Banned words: `class`, `delete`, `order`.
- Exception: `value_type` is allowed as an explicit approved key-value column name.

## Required Startup-Core Tables (v1)
- `core_users_profiles`
- `core_workstations_registry`
- `auth_roles_catalog`
- `auth_roles_assignments`
- `auth_sessions_tokens`
- `ops_startup_logs`
- `config_settings_values`
- `config_settings_history`

## Security and Session Rules
- Never store plaintext tokens.
- Store salted token hashes and metadata only.
- Session validity must be comparable to DB server UTC (for example, `fn_server_utc_now()` or `utc_timestamp()`).

## Data Lifecycle
- Hard delete policy only for primary entities.
- Keep auditability through explicit history tables (for example, `*_history`).
- Retention windows must be settings-driven from `config_settings_values`.

## Migration and Deployment
- Migration model: FluentMigrator runner backed by reviewed SQL artifacts.
- Use a file-per-artifact layout under `Database/`:
	- `Bootstrap/create_database.sql`
	- `Tables/<table_name>/create.sql` and `rollback.sql`
	- `StoredProcedures/<procedure_name>/create.sql` and `rollback.sql`
	- `Views/<view_name>/create.sql` and `rollback.sql`
	- `Seeds/<seed_name>/create.sql` and `rollback.sql`
	- `Validation/<validation_name>/validate.sql`
- Every schema artifact requires matching rollback or validation where appropriate.
- Production rollout is manual DBA-reviewed promotion.
- Do not auto-apply schema changes on app startup.

## PR Compliance
- SQL naming convention drift must fail PR validation.
- Any exception requires explicit written approval in migration PR notes.
