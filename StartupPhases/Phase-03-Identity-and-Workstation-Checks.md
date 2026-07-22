# Phase 03 - Identity and Workstation Checks

## Current Implementation Status (As of 2026-07-22)

- Status: Not Started
- Implemented:
	- Startup context has placeholders for username and role fields.
- Missing:
	- Database-backed user lookup by Windows username.
	- Workstation lookup by hostname plus MAC.
	- Explicit unknown workstation branch with New User action routing.
	- Role resolution sourced from database role values.
- Evidence:
	- `Services/StartupCoordinator.cs`, `Models/StartupState.cs`

## Goal

Implement startup identity lookups and workstation registration checks.

## Scope

- Match Windows username against database user record.
- Validate workstation using hostname plus MAC as composite key.
- Enforce no manual override for unknown workstation.
- Resolve admin/developer determination from database role values.

## Implementation Tasks

- Create and organize startup schema artifacts under `./Database` (users, workstations, sessions).
- Add user lookup query using Windows username.
- Add workstation lookup query using hostname and MAC.
- Add explicit startup branch for unregistered workstation that routes to Login first and exposes a New User action.
- Ensure no manual override path is exposed.

## Suggested NuGet Packages (If Relevant)

- MySqlConnector (MySQL connectivity)
- Dapper (lightweight SQL mapping)

## Done When

- Startup context includes `userMatched` and `workstationMatched` results.
- Unknown workstation always follows defined route logic with no override.

## Testable End State

Manual tests:

1. Known user plus known workstation returns matched state.
2. Unknown user plus known workstation routes to Login branch.
3. Unknown workstation routes to Login first with a New User option and does not offer an override action.
