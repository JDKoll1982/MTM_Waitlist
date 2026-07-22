# Phase 09 - Role Enforcement and Final Polish

## Current Implementation Status (As of 2026-07-22)

- Status: Not Started
- Implemented:
	- Startup state tracks a current role string and developer check helper.
- Missing:
	- Explicit role gates for Waitlist and Work Stations module actions.
	- Complete startup/admin control gating enforcement matrix.
	- Accessibility/error-copy hardening and full routing-matrix regression pass.
- Evidence:
	- `Models/StartupState.cs`

## Goal

Enforce role boundaries and complete final startup hardening checks.

## Scope

- Apply RBAC in Waitlist and Work Stations modules.
- Validate startup error copy and accessibility behavior.
- Verify end-to-end startup and diagnostics journey.
- Enforce role identifiers: Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer.
- Restrict startup-required administrative controls to Developer only.

## Implementation Tasks

- Enforce role gates for Waitlist actions.
- Enforce role gates for Work Stations actions.
- Validate Splash and error messages remain end-user-facing.
- Confirm alert design does not rely on color alone.
- Run full startup regression against the routing matrix.

## Suggested NuGet Packages (If Relevant)

- Microsoft.AspNetCore.Authorization (policy and role-based authorization primitives)
- Microsoft.Extensions.DependencyInjection.Abstractions (authorization service registration support)
- xunit (optional automated regression tests)
- FluentAssertions (optional readable test assertions)

## Done When

- Unauthorized actions are blocked in both modules.
- Startup and diagnostics flow match specification end-to-end.

## Testable End State

Manual tests:

1. Verify each role can only perform allowed module actions.
2. Verify restricted actions are blocked with user-friendly messaging.
3. Execute full startup path tests and confirm all final routes are correct.
