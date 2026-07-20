# Phase 05 - Session Validation and Routing

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
