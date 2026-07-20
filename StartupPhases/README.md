# MTM Waitlist App: Complete Startup Implementation Workflow

## File Directory

- Source of Truth Spec: [../StartupSpec.md](../StartupSpec.md)
- Phase Index: [README.md](README.md)
- Phase 01: [Phase-01-Startup-Shell-and-Splash.md](Phase-01-Startup-Shell-and-Splash.md)
- Phase 02: [Phase-02-Environment-and-Config.md](Phase-02-Environment-and-Config.md)
- Phase 03: [Phase-03-Identity-and-Workstation-Checks.md](Phase-03-Identity-and-Workstation-Checks.md)
- Phase 04: [Phase-04-Database-Failure-UX-and-Retry.md](Phase-04-Database-Failure-UX-and-Retry.md)
- Phase 05: [Phase-05-Session-Validation-and-Routing.md](Phase-05-Session-Validation-and-Routing.md)
- Phase 06: [Phase-06-Recovery-Flows-and-Data-Repair.md](Phase-06-Recovery-Flows-and-Data-Repair.md)
- Phase 07: [Phase-07-Logging-Pipeline-and-Retention.md](Phase-07-Logging-Pipeline-and-Retention.md)
- Phase 08: [Phase-08-Developer-Mode-and-Export-Reports.md](Phase-08-Developer-Mode-and-Export-Reports.md)
- Phase 09: [Phase-09-Role-Enforcement-and-Final-Polish.md](Phase-09-Role-Enforcement-and-Final-Polish.md)

## Project Overview: Startup Resilience
This workflow documents the 9-phase implementation of the MTM Waitlist application's startup engine. The system is built on a non-blocking architecture that prioritizes identity validation, data integrity, and enterprise-grade observability.

## Clarified Decisions (Applied Across All Phases)

- Splash first screen: Use a new standalone Splash window as the first visible screen at app launch.
- Startup database schema: No final schema exists yet. Create it and place each file type in the appropriate subfolder under `./Database`.
- Server time source: Use a database function for startup server-time validation.
- Session token source priority: Use both local storage and database sources, with local data taking precedence.
- Admin/developer determination: Resolve from the user's role stored in the database.
- Centralized logging destinations: None are confirmed right now. If existing targets are discovered, migrate them to the new logging workflow.
- MVP startup log format: Use plaintext JSONL.
- Logging target prompt cancellation: If admin/developer setup is canceled, startup must stop.
- Current RBAC role identifiers: Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer.
- Startup admin controls access: Developer role only.
- Unknown workstation routing: Show Login first, including a New User button.

Key Architectural Anchors:

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

- Tasks: Wire app launch to the Splash Screen route. All services must use a dedicated DI injector service.
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
