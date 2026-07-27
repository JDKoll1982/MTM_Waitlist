# Phase 03 - Identity and Workstation Checks

## Current Implementation Status (As of 2026-07-26)

- Status: In Progress
- Implemented:
	- Database-backed startup session repository resolves workstation registration using hostname plus MAC and user identity by normalized Windows username.
	- Startup coordinator now consumes repository results for identity routing and role resolution.
	- `New User` routing is now gated by authoritative workstation registration status to avoid false unregistered-workstation states when startup cannot verify DB status.
	- Startup runtime context now persists `IsWorkstationRegistrationAuthoritative` for explicit verification-state tracing.
	- Startup coordinator now validates malformed `StartupDatabaseOptions.ConnectionString` values at runtime and blocks with a deterministic startup-configuration message.
	- Startup DB connection string now supports environment-variable override (`MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING`) for packaged and unpackaged rollout consistency.
	- Baseline startup schema, seed artifacts, bootstrap script, and validation script now exist under per-artifact folders in `Database/`.
- Missing:
	- Production environment DB connection-value rollout (actual environment provisioning) across packaged and unpackaged launch profiles.
	- End-to-end role and workstation verification against live production-like data sets.
- Evidence:
	- `Services/StartupSessionRepository.cs`, `Services/StartupCoordinator.cs`, `Models/StartupSessionSnapshot.cs`
	- `Models/StartupState.cs`
	- `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs` (`RunAsync_WhenUnknownWorkstation_RoutesToLoginAndRequiresNewUserActionAsync`, `RunAsync_WhenWorkstationStatusIsNotAuthoritative_RoutesToLoginWithoutNewUserActionAsync`, `RunAsync_WhenDatabaseConnectionStringIsMalformed_ReturnsBlockedAsync`, `RunAsync_WhenConnectionStringEnvironmentOverrideIsMalformed_ReturnsBlockedAsync`, `RunAsync_WhenConnectionStringEnvironmentOverrideIsValid_IgnoresMalformedConfiguredConnectionStringAsync`)
	- `Database/Bootstrap/create_database.sql`, `Database/Tables/auth_roles_catalog/create.sql`, `Database/Tables/core_users_profiles/create.sql`, `Database/Tables/auth_roles_assignments/create.sql`, `Database/Tables/core_workstations_registry/create.sql`, `Database/Tables/auth_sessions_tokens/create.sql`, `Database/Tables/config_settings_values/create.sql`, `Database/Tables/config_settings_history/create.sql`, `Database/Tables/ops_startup_logs/create.sql`, `Database/StoredProcedures/fn_server_utc_now/create.sql`, `Database/Seeds/seed_dev_masked_baseline/create.sql`, `Database/Validation/startup_schema/validate.sql`

## Sequencing Note (2026-07-26)

- This phase is a required blocker for Supervisor Analytics implementation in `Documents/Analytics/Plan.md`.
- Analytics implementation starts only after startup phases 01 through 09 are complete.

## Goal

Implement startup identity lookups and workstation registration checks.

## Scope

- Match Windows username against database user record.
- Validate workstation using hostname plus MAC as composite key.
- Enforce no manual override for unknown workstation.
- Resolve admin/developer determination from database role values.

## Implementation Tasks

- Create and organize startup schema artifacts under `./Database` (users, workstations, sessions).
- Add user lookup query using Windows username.
- Add workstation lookup query using hostname and MAC.
- Add explicit startup branch for unregistered workstation that routes to Login first and exposes a New User action only when registration status is authoritative.
- Ensure no manual override path is exposed.

## Suggested NuGet Packages (If Relevant)

- MySqlConnector (MySQL connectivity)
- Dapper (lightweight SQL mapping)

## Done When

- Startup context includes `userMatched` and `workstationMatched` results.
- Startup context includes authoritative verification state for workstation registration.
- Unknown workstation routing follows authoritative-status rules with no override.

## Testable End State

Manual tests:

1. Known user plus known workstation returns matched state.
2. Unknown user plus known workstation routes to Login branch.
3. Unknown workstation routes to Login first with a New User option and does not offer an override action.
4. Malformed startup DB connection string blocks startup with a configuration error message.
5. A valid `MTM_WAITLIST_STARTUP_DB_CONNECTION_STRING` environment override takes precedence over appsettings connection string values.
