# Phase 07 - Logging Pipeline and Retention

## Current Implementation Status (As of 2026-07-26)

- Status: Complete
- Implemented:
	- Producer-consumer startup logging pipeline using a bounded in-memory channel and `BackgroundService` worker.
	- Hosted VM JSONL daily file sink (`startup_daily_YYYY_MM_DD.jsonl`) with SHA-256 hash chaining (`hash` + `previousHash`) per event.
	- Asynchronous retention cleanup (14-day expiry and 250 MB cap enforcement) running in the background worker.
	- Forwarder abstraction with bounded retry; default implementation forwards JSONL to configured centralized destination path when configured.
	- In-app developer destination-selection prompt UX implemented via WinUI `ContentDialog` with explicit cancel action.
	- Destination-selection prompt now auto-opens on startup when required (no retry click dependency).
	- Prompt includes compact browse action (`...`) with folder picker integration and end-user-facing copy.
	- Centralized destination startup gate implemented; destination can be persisted locally and used by startup coordinator and log forwarder.
	- Startup logging services and options are now registered in DI and hosted with the app host lifecycle.
	- Existing startup trace calls now flow through the pipeline while preserving debug output visibility.
- Verification:
	- `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug` passed (`35` total, `0` failed) after aligning test host to `win-x64`.
	- `MTM_Waitlist.Tests.csproj` is pinned to `x64` + `win-x64` to prevent Windows App SDK host initialization mismatch (`0x8007000B`) during test runs.
- Evidence:
	- `Services/StartupLogService.cs`, `Services/StartupLogForwarder.cs`, `Contracts/Services/IStartupLogService.cs`, `Contracts/Services/IStartupLogForwarder.cs`
	- `Helpers/StartupDebugLog.cs`, `App.xaml.cs`, `Services/ServiceRegistrationExtensions.cs`, `Services/StartupCoordinator.cs`
	- `Models/StartupLoggingOptions.cs`, `appsettings.json`

## Sequencing Note (2026-07-26)

- This phase is a required blocker for Supervisor Analytics implementation in `Documents/Analytics/Plan.md`.
- Analytics implementation starts only after startup phases 01 through 09 are complete.

## Goal

Implement non-blocking startup logging with hosted VM retention rules.

## Scope

- Non-blocking producer-consumer logging pattern.
- Hosted VM write target plus centralized forward.
- 14-day retention and 250 MB cap cleanup.
- Use plaintext JSONL for MVP startup logs.
- If any legacy logging targets exist, migrate them to this new workflow.

## Implementation Tasks

- Add in-memory log queue or channel with background worker.
- Ensure UI thread does not perform sync I/O for startup logs.
- Write logs to configured hosted VM logging path.
- Forward logs to centralized logging destination.
- Handle the current state where no centralized destination is confirmed yet.
- If admin/developer is prompted to configure destination and cancels, stop startup.
- Implement time and size retention cleanup asynchronously.

## Suggested NuGet Packages (If Relevant)

- Serilog (structured logging pipeline)
- Serilog.Sinks.File (file sink for hosted VM path)
- Serilog.Sinks.Async (non-blocking async sink wrapper)
- Serilog.Extensions.Hosting (host integration)

## Done When

- Startup remains responsive under logging load.
- Hosted VM retention limits are enforced automatically.

## Testable End State

Manual tests:

1. Start app with slow or unstable logging target and confirm splash remains responsive.
2. Populate hosted VM log folder over 250 MB and confirm oldest files are purged.
3. Add files older than 14 days and confirm cleanup removes them.
