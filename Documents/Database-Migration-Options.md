# Database Migration and Validation Options

## Goal

Find a NuGet package and file structure that can:

- create the database if it does not exist
- apply schema updates in order
- validate the current database against the expected schema
- support rollback scripts
- work safely with the existing MTM Waitlist MySQL schema

## Recommended Package Candidates

### 1. FluentMigrator

- NuGet package: `FluentMigrator`
- Context7 library: `/fluentmigrator/fluentmigrator`
- Best fit when you want a structured migration engine without forcing a monolithic migrations folder
- Supports `MigrateUp`, `MigrateDown`, rollback, and transaction control
- Good match for a schema-first workflow where the repo owns the source of truth
- Strongest option if we want a clean runner while keeping each table/procedure/view file separate

### 2. DbUp

- NuGet package: `DbUp`
- Best fit when you want simple ordered SQL script execution
- Good for bootstrapping and incremental SQL deployment
- Easier than a full migration framework, but weaker for rich rollback and schema introspection
- Good option if we want to keep everything as SQL files with minimal code

### 3. Liquibase

- NuGet package: Liquibase-based .NET integrations or CLI wrapper depending on hosting choice
- Best fit when you want full change tracking, checksums, and database-agnostic migrations
- Strong enterprise workflow, but heavier than needed for this repo unless we want cross-platform governance
- Better as a reference option than the primary fit for this project

## Short Recommendation

Use **FluentMigrator** if we want a proper .NET migration pipeline with rollback support and a clean long-term structure.
Use **DbUp** if we want the simplest SQL-script-first path.

For MTM Waitlist, **FluentMigrator** is the best overall fit because it gives us:

- install/update style execution
- rollback support
- versioned schema control
- a path to validation and bootstrap logic without hand-rolled SQL parsing

## Requirements We Need To Cover

- [ ] Create the database if it does not exist
- [ ] Apply baseline schema in a known order
- [ ] Keep rollback scripts alongside migration scripts
- [ ] Validate the actual database against the expected schema
- [ ] Support a local server and a work server target
- [ ] Allow the build or workflow to try multiple connection secrets
- [ ] Skip cleanly when no connection secrets are configured
- [ ] Keep MySQL 5.7 compatibility
- [ ] Preserve lowercase snake_case naming rules
- [ ] Avoid auto-applying schema changes on app startup

## Current Database File Structure

The repo now uses a file-per-artifact structure:

### Current top-level structure

- `Database/Bootstrap/` for database creation only
- `Database/Tables/<table_name>/` for table creation and rollback
- `Database/StoredProcedures/<procedure_name>/` for stored procedures and rollback
- `Database/Views/<view_name>/` for views and rollback
- `Database/Seeds/<seed_name>/` for masked synthetic seed data and rollback
- `Database/Validation/<validation_name>/` for schema validation checks

### Example layout

- `Database/Bootstrap/create_database.sql`
- `Database/Tables/auth_roles_catalog/create.sql`
- `Database/Tables/auth_roles_catalog/rollback.sql`
- `Database/Tables/core_users_profiles/create.sql`
- `Database/Tables/core_users_profiles/rollback.sql`
- `Database/StoredProcedures/fn_server_utc_now/create.sql`
- `Database/StoredProcedures/fn_server_utc_now/rollback.sql`
- `Database/Seeds/seed_dev_masked_baseline/create.sql`
- `Database/Seeds/seed_dev_masked_baseline/rollback.sql`
- `Database/Validation/startup_schema/validate.sql`

### Optional future refinement

If we choose FluentMigrator, we can later move from raw SQL files to either:

- C# migration classes, or
- a hybrid model where the runner executes reviewed SQL files by artifact folder

## Suggested Next Step

Before changing any schema structure, decide which direction we want:

1. **FluentMigrator** for a structured migration runner
2. **DbUp** for simple SQL script execution
3. **Keep SQL files** and only add a stronger validator/bootstrap runner

## Notes

- The database name in this repo is `mtm_waitlist`.
- The current startup schema is MySQL 5.7 compatible.
- The workflow/build logic should never assume the database already exists.
