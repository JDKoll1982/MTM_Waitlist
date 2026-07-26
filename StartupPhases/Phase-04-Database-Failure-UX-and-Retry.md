# Phase 04 - Database Failure UX and Retry

## Current Implementation Status (As of 2026-07-26)

- Status: In Progress
- Implemented:
	- Startup DB calls now enforce a 10-second connection timeout and bounded retry policy (max 2 retries with exponential backoff base delay).
	- Splash startup flow now identifies DB-specific startup failures and limits actions to manual retry or close-app semantics (no reset-to-defaults action for DB outages).
	- Splash retry after DB failure now re-runs DB phase checks only (`retryDatabasePhaseOnly`) instead of re-running local-settings repair stage.
- Missing:
	- End-to-end validation against real network drop and DB-down scenarios in packaged launch profile.
- Evidence:
	- `Models/StartupDatabaseOptions.cs`, `Services/StartupSessionRepository.cs`, `Services/StartupCoordinator.cs`
	- `ViewModels/SplashViewModel.cs`, `Views/SplashView.xaml`
	- `MTM_Waitlist.Tests/ViewModels/SplashViewModelTests.cs`, `MTM_Waitlist.Tests/Services/StartupCoordinatorTests.cs`

## Sequencing Note (2026-07-26)

- This phase is a required blocker for Supervisor Analytics implementation in `Documents/Analytics/Plan.md`.
- Analytics implementation starts only after startup phases 01 through 09 are complete.

## Goal

Implement database failure handling exactly as defined by spec.

## Scope

- 10 second timeout with bounded retry policy.
- Splash error state with `Retry` and `Close App`.
- No automatic retry loops.
- Keep failure handling manual and blocking until the user retries or exits.

## Implementation Tasks

- Apply DB timeout and retry limits in startup database calls.
- Implement Splash error state UI for DB failure.
- Wire `Retry` to re-run DB phase only.
- Wire `Close App` to safe application exit.

## Suggested NuGet Packages (If Relevant)

- MySqlConnector (timeouts and retry-aware DB calls)
- Polly (resilience policies for bounded retry/backoff)

## Done When

- DB failures do not crash app.
- User can only retry manually or close app.

## Automated Coverage

1. `MTM_Waitlist.Tests.ViewModels.SplashViewModelTests.StartAsync_WhenDatabaseFailureBlocked_HidesResetActionAndShowsManualActionsAsync`
2. `MTM_Waitlist.Tests.ViewModels.SplashViewModelTests.RetryAsync_AfterDatabaseFailure_RerunsDatabasePhaseOnlyAsync`

## Testable End State

Manual tests:

1. Disconnect DB target and launch app.
2. Confirm Splash shows DB error state with `Retry` and `Close App`.
3. Confirm app does not auto retry in a loop.
