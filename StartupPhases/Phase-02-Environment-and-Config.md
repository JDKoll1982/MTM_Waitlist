# Phase 02 - Environment and Config

## Goal

Implement Phase 1 environment checks and configuration load behavior.

## Scope

- Read Windows username.
- Load app configuration.
- Apply reset flow entry points (`Try Again`, `Reset to Defaults`).
- Prepare startup context fields consumed by downstream role resolution from the database.

## Implementation Tasks

- Read `%USERNAME%` and store it in startup context.
- Load required startup config.
- If config fails, show dialog with `Try Again` and `Reset to Defaults`.
- Implement targeted setting reset first, full reset fallback second.
- Ensure config/bootstrap paths support upcoming schema assets under `./Database`.

## Suggested NuGet Packages (If Relevant)

- Microsoft.Extensions.Configuration.Json (JSON config loading)
- Microsoft.Extensions.Options.ConfigurationExtensions (typed settings binding)
- Microsoft.Extensions.Configuration.Binder (strongly typed config binding)

## Done When

- Username and config are available to downstream startup phases.
- Failure path uses user-facing recovery actions.

## Testable End State

Manual tests:

1. Run with valid config and confirm phase completes.
2. Corrupt one setting and confirm targeted reset path is attempted.
3. Force unrecoverable config issue and confirm full reset path is offered.
