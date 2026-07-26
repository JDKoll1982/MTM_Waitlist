# Phase 08 - Developer Mode and Export Reports

## Current Implementation Status (As of 2026-07-22)

- Status: Not Started
- Implemented:
	- Developer mode visibility toggling exists in shell-level UI wiring.
- Missing:
	- Credential-gated Developer Mode access flow.
	- Startup/runtime log viewer page.
	- HTML diagnostic export service, naming convention, directory creation, and success toast shortcut.
- Evidence:
	- `Views/ShellPage.xaml.cs` (developer visibility check)

## Sequencing Note (2026-07-26)

- This phase is a required blocker for Supervisor Analytics implementation in `Documents/Analytics/Plan.md`.
- Analytics implementation starts only after startup phases 01 through 09 are complete.

## Goal

Deliver Developer Mode read and export workflows.

## Scope

- Developer credential-gated access.
- In-app log viewing.
- HTML report export with naming and toast feedback.
- Restrict startup-required administrative controls to the Developer role.

## Implementation Tasks

- Add Developer Mode entry point and access gate.
- Prompt for developer credentials when current session is not a developer.
- Add log viewer page for startup and runtime logs.
- Export workflow report to `%USERPROFILE%\\Downloads\\MTM_Waitlist_Diagnostic_Reports\\`.
- Apply report filename format: `WorkflowReport_{Hostname}_{WindowsUsername}_YYYYMMDD_HHmmss.html`.
- Create export directory automatically if missing.
- Show non-blocking toast with folder shortcut on successful export.

## Suggested NuGet Packages (If Relevant)

- Scriban (HTML template rendering for workflow reports)
- No additional package required for folder creation and file writes (built-in .NET APIs)
- No additional package required for app notifications if using Windows App SDK notification APIs already in project

## Done When

- Only authorized developers can access Developer Mode.
- Exported report files are readable, named correctly, and easy to locate.

## Testable End State

Manual tests:

1. Sign in as non-developer and verify credential prompt appears before access.
2. Export a report and confirm directory auto-creation on first export.
3. Confirm exported filename follows required format and toast shortcut opens folder.
