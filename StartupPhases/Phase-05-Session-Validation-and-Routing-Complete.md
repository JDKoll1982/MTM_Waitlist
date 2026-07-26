# Phase 05 - Session Validation and Routing

## Current Implementation Status (As of 2026-07-22)

- Status: Completed (Implementation Ready; integration depends on configured DB connection)
- Implemented:
	- Startup now uses explicit route targets for both session-valid (`Waitlist`) and session-invalid/unknown-user (`Login`) outcomes.
	- Session token arbitration is implemented with local-first precedence over database token source values.
	- Session validity is checked against server time from database function `fn_server_utc_now()` through a startup session repository.
	- Unknown workstation and unmatched user branch now sets a `New User` action requirement state used by the login route.
	- Login `New User` action now persists a startup registration request payload for follow-up startup controls.
	- Splash startup now receives and displays the finalized five-step approved progress strings.
- Missing:
	- Production connection string and database tables/functions must be configured in deployment environment (`StartupDatabaseOptions.ConnectionString`).
- Evidence:
	- `Services/StartupCoordinator.cs`, `Services/StartupSessionRepository.cs`, `Services/StartupRegistrationService.cs`, `ViewModels/SplashViewModel.cs`, `ViewModels/LoginViewModel.cs`, `Views/LoginPage.xaml`

## Sequencing Note (2026-07-26)

- This phase is a required blocker for Supervisor Analytics implementation in `Documents/Analytics/Plan.md`.
- Analytics implementation starts only after startup phases 01 through 09 are complete.

## Goal

Complete startup routing using server-time session validation.

## Scope

- Validate session token against server time from a DB function.
- Validate session token data from both local storage and database, with local data taking precedence.
- Route to Main Window or Login (with New User available from Login for unknown users/workstations).
- Show finalized splash progress strings during execution.

## Implementation Tasks

- Add server-time session validity check using a DB function.
- Implement dual-source token evaluation (local plus database) with local-first precedence.
- Implement routing matrix from startup context outcomes.
- Add all five approved user-facing progress step strings.
- Ensure startup sequence reaches one final route target.

## Suggested NuGet Packages (If Relevant)

- System.IdentityModel.Tokens.Jwt (if session token is JWT-based)
- Microsoft.Extensions.Caching.Memory (session/cache state handling)

## Done When

- Route outcomes match spec for all combinations.
- Splash progress text is non-technical and complete.

## Testable End State

Manual tests:

1. Matched user plus valid session routes to Main Window.
2. Matched user plus invalid session routes to Login.
3. Unmatched user plus unregistered workstation routes to Login first, with New User available.
4. Confirm step text shows all five approved strings in sequence.
