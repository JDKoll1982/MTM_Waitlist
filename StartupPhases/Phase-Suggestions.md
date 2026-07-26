# Startup Implementation Suggestions

## Current Implementation Status (As of 2026-07-22)

- Status: Active Backlog
- Summary:
  - This file remains in-progress by design and tracks follow-on engineering opportunities.
  - Suggestions here are not all implemented and should be re-evaluated at the start of each phase.

This file captures implementation suggestions discovered while executing startup phases.

## Sequencing Rule (2026-07-26)

- Startup phases 01 through 09 remain the active delivery focus.
- Supervisor Analytics work from `Documents/Analytics/Plan.md` is intentionally deferred until Phase 09 is complete.

## Suggestion 1

- Phase observed: Phase 1 (Startup Shell and Splash)
- Suggestion: Add an app-level startup router service to own transitions from Splash to Login/Main instead of triggering route changes directly inside the Splash view model.
- Why: Keeping routing in a dedicated service prevents UI coupling and makes later phase routing matrix changes easier to test and maintain.
- Where to implement:
  - `Contracts/Services/IStartupRoutingService.cs`
  - `Services/StartupRoutingService.cs`
  - Integrate from `Services/StartupCoordinator.cs`

## Suggestion 2

- Phase observed: Phase 2 (Environment and Configuration)
- Suggestion: Add structured startup event logging for each reset action (`TryAgain`, targeted reset, full reset), including outcome and corrupt-setting identifier.
- Why: Phase 2 recovery behavior is user-visible and should be auditable for diagnostics and support workflows.
- Where to implement:
  - `Contracts/Services/IStartupEventLogger.cs`
  - `Services/StartupEventLogger.cs`
  - Emit events from `Services/StartupCoordinator.cs`

## Suggestion 3

- Phase observed: Phase 2 (Environment and Configuration)
- Suggestion: Add a small startup validation profile object to centralize required config keys and validation rules.
- Why: Validation logic is currently inline and will grow in later phases; centralizing rules reduces duplication and regression risk.
- Where to implement:
  - `Models/StartupValidationProfile.cs`
  - `Services/StartupConfigurationValidator.cs`
  - Call from `Services/StartupCoordinator.cs`

## Suggestion 4

- Phase observed: Phase 4 (Database Failure UX and Retry)
- Suggestion: Replace simulated startup DB access with a production repository using `MySqlConnector` and explicit timeout/retry telemetry fields.
- Why: Current phase logic enforces policy, but production behavior requires real query execution and richer retry diagnostics.
- Where to implement:
  - `Contracts/Services/IStartupDatabaseRepository.cs`
  - `Services/StartupDatabaseRepository.cs`
  - Inject into `Services/StartupDatabaseService.cs`

## Suggestion 5

- Phase observed: Phase 5 (Session Validation and Routing)
- Suggestion: Move session-token source arbitration (local-first vs database) into a dedicated resolver service and persist decision metadata for diagnostics.
- Why: Routing decisions depend on token precedence and server time, and this logic should remain testable and isolated.
- Where to implement:
  - `Contracts/Services/IStartupSessionResolver.cs`
  - `Services/StartupSessionResolver.cs`
  - Integrate in `Services/StartupCoordinator.cs`

## Suggestion 6

- Phase observed: Phase 6 (Recovery Flows and Data Repair)
- Suggestion: Record duplicate-record cleanup actions as structured startup events including affected record IDs and retained record timestamp.
- Why: Silent repair is required for UX continuity, but support teams still need traceability for audit and debugging.
- Where to implement:
  - `Services/StartupCoordinator.cs`
  - `Services/StartupLogService.cs`

## Suggestion 7

- Phase observed: Phase 7 (Logging Pipeline and Retention)
- Suggestion: Add a central-forwarder abstraction for hosted VM -> centralized destination delivery with bounded retry and dead-letter capture.
- Why: Current log file persistence and retention are implemented, but reliable forwarding requires a separable transport strategy.
- Where to implement:
  - `Contracts/Services/IStartupLogForwarder.cs`
  - `Services/StartupLogForwarder.cs`
  - Integrate into `Services/StartupLogService.cs`

## Suggestion 8

- Phase observed: Phase 8 (Developer Mode and Export Reports)
- Suggestion: Add an in-app diagnostic report catalog page with export history, open-folder action, and report status badges.
- Why: Export generation exists, but discoverability and repeat troubleshooting improve when reports are visible in-app.
- Where to implement:
  - `Views/DeveloperDiagnosticsPage.xaml`
  - `ViewModels/DeveloperDiagnosticsViewModel.cs`
  - `Services/StartupDiagnosticExportService.cs`

## Suggestion 9

- Phase observed: Phase 9 (Role Enforcement and Final Polish)
- Suggestion: Formalize role-action policies into a configuration-driven matrix and validate it at startup.
- Why: Policy drift is likely as modules grow; a single matrix source reduces inconsistencies.
- Where to implement:
  - `Models/StartupRolePolicyOptions.cs`
  - `Services/StartupRolePolicyService.cs`
  - `appsettings.json`

## Suggestion 10

- Phase observed: Phase 5 (Session Validation and Routing)
- Suggestion: Store local session tokens with platform-protected encryption and add token integrity metadata.
- Why: Local-first precedence is implemented, but plaintext token storage increases replay/tampering risk.
- Where to implement:
  - `Services/LocalSettingsService.cs`
  - `Contracts/Services/ILocalSettingsService.cs`
  - `Models/StartupSessionToken.cs`

## Suggestion 11

- Phase observed: Post-Phase 09 handoff
- Suggestion: Start analytics implementation only after the startup status table in `StartupPhases/README.md` shows all phases complete.
- Why: This prevents startup hardening work from being diluted by unrelated analytics scope.
- Where to implement:
  - `Documents/Analytics/Plan.md`
  - `StartupPhases/README.md` (status verification checkpoint)
