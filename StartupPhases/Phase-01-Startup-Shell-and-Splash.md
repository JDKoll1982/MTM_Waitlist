# Phase 01 - Startup Shell and Splash

## Goal

Create the startup shell that always begins with the Splash Screen and owns startup sequencing.

## Scope

- Launch app into Splash Screen first.
- Add startup state container used by later phases.
- Show progress area and status text placeholder.

## Implementation Tasks

- Wire app launch to Splash Screen route.
- All Services should use DI and the DI service (the injector) should be in its own service not in the main app file
- Add startup coordinator interface and empty implementation.
- Add startup result model (success, blocked, route target).
- Add Splash Screen layout placeholders for status and actions.

## Suggested NuGet Packages (If Relevant)

- CommunityToolkit.Mvvm (ViewModel and command patterns)
- Microsoft.Extensions.Hosting (hosted startup orchestration)
- Microsoft.Extensions.DependencyInjection (DI registration and resolution)

## Done When

- App always opens Splash Screen before any route decision.
- Startup coordinator is called from Splash Screen.

## Testable End State

Manual test:

1. Launch app.
2. Confirm Splash Screen is the first visible screen.
3. Confirm no direct route to Main Window or Login happens yet.

## Clarified Decisions (Pre-Phase 1)

- Splash first screen: Use a new standalone Splash window as the first visible screen at app launch.
- Startup database schema: No final schema exists yet. Create it and place each file type in the appropriate subfolder under `./Database`.
- Server time source: Use a database function for startup server-time validation.
- Session token storage and format: Store and validate session token data in both local storage and the database, with local data taking precedence over database data.
- Admin/developer determination: Resolve from the user's role stored in the database.
- Centralized logging destination support: No supported destinations are confirmed yet. If any existing destinations are found, update them to the new logging workflow.
- MVP startup log format: Use plaintext JSONL.
- Admin/developer logging prompt cancel behavior: If destination setup is canceled, startup must stop.
- RBAC role identifiers (current): Material Handler, Production, Production Lead, Setup, Setup Lead, Plant Manager, Developer.
- Startup admin controls access: Developer role only.
- Unknown workstation routing: Show Login first, including a New User button.

## Cross-Phase Alignment Notes

- This phase uses a standalone Splash window as the startup entry point.
- Startup administration controls introduced by later phases are restricted to the Developer role.