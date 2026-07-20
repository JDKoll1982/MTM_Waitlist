# Phase 04 - Database Failure UX and Retry

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

## Testable End State

Manual tests:

1. Disconnect DB target and launch app.
2. Confirm Splash shows DB error state with `Retry` and `Close App`.
3. Confirm app does not auto retry in a loop.
