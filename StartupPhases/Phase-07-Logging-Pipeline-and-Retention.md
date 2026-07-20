# Phase 07 - Logging Pipeline and Retention

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
