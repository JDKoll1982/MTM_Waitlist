# MTM Waitlist App: Complete Startup Implementation Workflow

## File Directory

- Source of Truth Spec: [../StartupSpec.md](../StartupSpec.md)
- Phase Index: [README.md](README.md)
- Phase 01: [Phase-01-Startup-Shell-and-Splash-Complete.md](Phase-01-Startup-Shell-and-Splash-Complete.md)
- Phase 02: [Phase-02-Environment-and-Config-Complete.md](Phase-02-Environment-and-Config-Complete.md)
- Phase 03: [Phase-03-Identity-and-Workstation-Checks.md](Phase-03-Identity-and-Workstation-Checks.md)
- Phase 04: [Phase-04-Database-Failure-UX-and-Retry.md](Phase-04-Database-Failure-UX-and-Retry.md)
- Phase 05: [Phase-05-Session-Validation-and-Routing-Complete.md](Phase-05-Session-Validation-and-Routing-Complete.md)
- Phase 06: [Phase-06-Recovery-Flows-and-Data-Repair.md](Phase-06-Recovery-Flows-and-Data-Repair.md)
- Phase 07: [Phase-07-Logging-Pipeline-and-Retention-Complete.md](Phase-07-Logging-Pipeline-and-Retention-Complete.md)
- Phase 08: [Phase-08-Developer-Mode-and-Export-Reports.md](Phase-08-Developer-Mode-and-Export-Reports.md)
- Phase 09: [Phase-09-Role-Enforcement-and-Final-Polish.md](Phase-09-Role-Enforcement-and-Final-Polish.md)

## Project Overview: Startup Resilience
This workflow documents the 9-phase implementation of the MTM Waitlist application's startup engine. The system is built on a non-blocking architecture that prioritizes identity validation, data integrity, and enterprise-grade observability.

## Current Implementation Status (As of 2026-07-22)

| File | Status | Notes |
|---|---|---|
| Phase 01 | Complete | Startup uses a standalone splash window and startup coordinator handoff. |
| Phase 02 | Complete | Username/config load, targeted single-setting remediation, and reset actions are implemented. |
| Phase 03 | In Progress | Database-backed username/workstation checks are implemented with authoritative workstation-status handling; final production hardening remains. |
| Phase 04 | In Progress | Bounded DB timeout/retry policy, manual DB-failure splash actions, and a dedicated DB-down banner are implemented; real-environment validation remains. |
| Phase 05 | Complete (Implementation) | Local-first session arbitration, DB-function-backed server-time/token integration, and login new-user request flow are implemented; deployment requires startup DB connection configuration. |
| Phase 06 | Not Started | Duplicate-record self-healing and repair flows are not implemented. |
| Phase 07 | Complete | Async JSONL pipeline, retention cleanup, forwarder, destination-selection prompt UX, and destination gate are implemented; `dotnet test` passes in win-x64 test host. |
| Phase 08 | Not Started | Developer export/report workflows are not implemented. |
| Phase 09 | Not Started | Final RBAC enforcement matrix and polish checks are not implemented. |
| Phase Suggestions | Active Backlog | Suggestion list exists and remains actionable. |

## Detailed Phase Checklist

### Phase 01 - Startup Shell and Splash
- [x] Wire app launch to a standalone Splash window startup entry point.
- [x] Add startup state container used by later phases.
- [x] Add startup coordinator interface and implementation.
- [x] Add startup result model for success, blocked, and route target outcomes.
- [x] Add Splash Screen layout placeholders for status and actions.
- [x] Defer main window activation until startup success handoff.
- [x] Use a compact centered splash footprint without title bar chrome or caption buttons.
- [ ] No remaining Phase 01 tasks.

### Phase 02 - Environment and Config
- [x] Read the Windows username into startup context.
- [x] Load required startup configuration values.
- [x] Validate local settings paths before proceeding.
- [x] Show recovery entry points from splash actions (`Try Again`, `Reset to Defaults`).
- [x] Attempt targeted single-setting remediation before full reset fallback.
- [x] Surface recovery text that distinguishes targeted repair from clearing all local settings.
- [ ] No remaining Phase 02 tasks.

### Phase 03 - Identity and Workstation Checks
- [x] Query the local host for hostname and MAC address using standard Windows APIs.
- [x] Match normalized Windows username against database user records.
- [x] Validate workstation using hostname plus MAC as a composite key.
- [x] Resolve user role from the database and flow it into startup context.
- [x] Gate `New User` routing behind authoritative workstation registration status.
- [x] Persist `IsWorkstationRegistrationAuthoritative` in startup runtime state.
- [x] Validate malformed startup DB connection strings before DB calls begin.
- [x] Support environment-variable override for startup DB connection strings.
- [ ] Validate role and workstation behavior against live production-like data sets.
- [ ] Confirm packaged and unpackaged rollout behavior in a real deployment environment.

### Phase 04 - Database Failure UX
- [x] Enforce a 10-second connection timeout for startup DB calls.
- [x] Enforce bounded retry policy with a maximum of 2 retries and exponential backoff.
- [x] Detect DB-specific startup failures and keep retrying manual only.
- [x] Re-run DB phase checks only when retry is invoked after DB failure.
- [x] Hide `Reset to Defaults` during DB-failure states.
- [x] Show a dedicated DB-unavailable banner in the splash UI.
- [x] Keep `Retry` and `Close App` available for DB-failure recovery.
- [ ] Validate real network drop and DB-down behavior in a packaged launch profile.

### Phase 05 - Session Validation and Routing
- [x] Validate session token against server time from the database function.
- [x] Use local session token data before database token data.
- [x] Route to Main Window when user match and session validity succeed.
- [x] Route to Login when session validation fails or user match is missing.
- [x] Surface `New User` as part of the Login branch for unknown workstation or user cases.
- [x] Show the finalized five-step splash progress mapping.
- [ ] No remaining Phase 05 tasks beyond configured deployment DB connection values.

### Phase 06 - Recovery Flows and Data Repair
- [ ] Detect duplicate user rows during identity load.
- [ ] Delete the oldest duplicate row and retain the newest record.
- [ ] Apply corrupted-setting remediation rules from startup reset flow.
- [ ] Continue startup after successful repair without exposing internal data errors.
- [ ] Validate recovery behavior against duplicate and corruption test data.

### Phase 07 - Logging Pipeline and Retention
- [x] Add asynchronous producer-consumer startup logging pipeline.
- [x] Write hosted VM logs as daily JSONL files.
- [x] Chain log entries with SHA-256 previous-hash integrity.
- [x] Run retention cleanup asynchronously with 14-day and 250 MB limits.
- [x] Forward logs to a configured centralized destination when available.
- [x] Auto-open the centralized destination prompt when required.
- [x] Allow developers to browse and persist a destination from startup UX.
- [x] Stop startup if destination setup is canceled for an admin/developer path.
- [x] Register logging services and host them with the app lifecycle.
- [ ] No remaining Phase 07 tasks.

### Phase 08 - Developer Mode and Export Reports
- [ ] Add a credential-gated Developer Mode access flow.
- [ ] Add a startup/runtime log viewer page.
- [ ] Add HTML diagnostic export service and template.
- [ ] Create the export directory automatically if missing.
- [ ] Use the required report filename format.
- [ ] Show a non-blocking success toast with folder shortcut after export.
- [ ] Confirm only authorized developers can access troubleshooting tools.

### Phase 09 - Role Enforcement and Final Polish
- [ ] Enforce role gates for Waitlist actions.
- [ ] Enforce role gates for Work Stations actions.
- [ ] Restrict startup administrative controls to Developer only.
- [ ] Validate splash and error messages remain end-user-facing.
- [ ] Ensure alerts do not rely on color alone.
- [ ] Run a full startup regression across the routing matrix.
- [ ] Confirm the final RBAC role set is enforced end-to-end.

## Execution Order Lock (2026-07-26)

- Complete startup phases 01 through 09 before beginning Supervisor Analytics page implementation.
- Treat `Documents/Analytics/Plan.md` as a post-startup workstream that starts only after Phase 09 is complete.
- Do not introduce analytics page UI, routing, services, or settings persistence into in-progress startup phases.

## Clarified Decisions (Applied Across All Phases)

- Splash first screen: Use a new standalone Splash window as the first visible screen at app launch.
- Startup database schema: Use a file-per-artifact layout under `./Database` with `Bootstrap`, `Tables`, `StoredProcedures`, `Seeds`, and `Validation` folders.
- Server time source: Use a database function for startup server-time validation.
- Session token source priority: Use both local storage and database sources, with local data taking precedence.
- Admin/developer determination: Resolve from the user's role stored in the database.
- Centralized logging destinations: None are confirmed right now. If existing targets are discovered, migrate them to the new logging workflow.
- MVP startup log format: Use plaintext JSONL.
- Logging target prompt cancellation: If admin/developer setup is canceled, startup must stop.
- Current RBAC role identifiers: Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer.
- Startup admin controls access: Developer role only.
- Unknown workstation routing: Show Login first. Only show `New User` when workstation status is authoritatively unregistered.

## Key Architectural Anchors:

- Primary Identity Source: The Windows environment username (`%USERNAME%`) is the absolute primary source value for identity checks.
- Workstation Validation: Uses standard Windows APIs to form a composite key (Hostname + MAC) for MySQL workstation validation.
- Developer Mode: Included in the MVP scope with full HTML export capabilities.
- Cloud PC Recovery: Excluded from MVP scope and treated as a future enhancement.

## Project-Wide Recommended Tech Stack Baseline
Based on the startup implementation requirements, these package versions are recommended as a baseline for security and performance.

| Category | Recommended Package | Baseline Version | Notes |
|---|---|---|---|
| Database | [MySqlConnector](https://www.nuget.org/packages/mysqlconnector) | 2.6.1 | No known vulnerabilities; replaces legacy MySql.Data. |
| Templating | [Scriban](https://www.nuget.org/packages/Scriban/529.0.0) | 7.2.5 | Safe. Versions 6.5.8 and earlier have critical vulnerabilities. |
| Resilience | [Polly](https://www.nuget.org/packages/Polly/509.0.0) | 8.7.0 | Current standard for retry and backoff logic. |
| Logging | [Serilog.Sinks.Async](https://www.nuget.org/packages/serilog.sinks.async) | 2.1.0 | Stable; essential for non-blocking UI thread logging. |
| Identity | [Microsoft.Extensions.Options](https://www.nuget.org/packages/microsoft.extensions.options.configurationextensions/) | 10.0.10 | Latest version released July 15, 2026. |

## Foundation: Shell, Environment and Identity

## Phase 01: Startup Shell and Splash
Establish the UI container and dependency injection (DI) foundation.

- Tasks: Wire app launch to a standalone Splash window startup entry point. All services must use a dedicated DI injector service.
- Splash Standard: All user-facing text must avoid code jargon and present every startup step clearly.

## Phase 02: Environment and Configuration
Initialize the local environment and recover from corrupted local settings.

- Settings UX Recovery: If Phase 1 fails, present "Try Again" or "Reset to Defaults."
- Remediation Rule: If a specific corrupt setting is identified, reset only that setting. Otherwise, reset all settings before re-running Phase 1.

## Phase 03: Identity and Workstation Checks
Validate the physical device and human actor against the database.

- Windows API Integration: Query the local host for Hostname and MAC address using standard Windows APIs.
- No Override Policy: If the workstation record is unknown, no manual override path is allowed.

## Logic: Resilience and Routing

## Phase 04: Database Failure UX
Handle network and database availability issues without crashing.

- Policy: 10-second connection timeout with a maximum of 2 retries using exponential backoff.
- UX: Failure must be shown as an error state within the Splash Screen. Automatic retrying is strictly prohibited.

## Phase 05: Session Validation and Routing
Evaluate the final destination based on session state and identity.

- Time Validation: Session validity must be checked against server time, not local system time.
- Progress Mapping: Use the exact five-step determinate progress mapping (e.g., "Step 2 of 5: Checking device registration...").

## Phase 06: Recovery and Data Repair
Implement self-healing for common database record anomalies.

- Duplicate User Mitigation: If multiple records exist for one user, silently delete the oldest record based on update timestamps and retain the newest.
- Continuity: Recovery must happen without exposing internal data errors to the end user.

## Operations: Observability and Security

## Phase 07: Logging Pipeline and Retention
Implement the asynchronous, producer-consumer logging system.

- Centralized Logging Rule: If the target is unset and the user is not an admin or developer, startup cannot continue. Admin and developer users must be prompted to set the destination before proceeding.
- Integrity: Use cryptographic chaining (hash of data + hash of previous item) to ensure log integrity.

## Phase 08: Developer Mode and Export
Provide authorized troubleshooting tools and printable reports.

- Access Gate: Prompt for developer username and password if the current session user does not have Developer Access.
- Export Standards: Workflow reports are exported in HTML format; redaction is not required for these diagnostic reports.

## Phase 09: Role Enforcement and Polish
Final hardening of role-based access control (RBAC).

- RBAC Matrix: Enforce role gates using the current role set (Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer) and keep startup administrative controls Developer-only.
- Inclusive UX: Alerts must not rely only on color; use iconography and strong text contrast.

## Final Completion Criteria
The full workflow is done when the application completes the end-to-end journey from Splash initialization through to the final startup routing target (Main Window or Login, with New User available from Login) while adhering to all logging, security, and recovery specifications.

After this completion criteria is met for Phase 09, the next implementation wave begins with `Documents/Analytics/Plan.md`.
