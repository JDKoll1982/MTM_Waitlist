---
name: 'WinUI 3 UI Automation'
description: 'Rules for driving the running MTM_Waitlist window with Windows UI Automation; the procedure itself lives in the winui3-ui-automation skill.'
applyTo: "**/*.{xaml,cs,ps1,resw}"
---

# WinUI 3 UI Automation — rules

The procedure moved to the `winui3-ui-automation` skill on 2026-11-26, so it loads only when a task
drives the app. These rules apply either way.

## Rules

- Verify the running app; do not add automation hooks to production code. If a control needs a stable
  hook, add `AutomationProperties.AutomationId` in XAML as the deliberate fix.
- Do not modify the build to make automation easier. Automate the same artifact users run.
- Run one automation session at a time, and avoid it while the user is typing. Automation takes focus and
  can move the cursor.
- Match privilege levels. A non-elevated shell cannot automate an elevated window. When this workstation
  needs administrator rights, use the account in `.github/memories/repo/workstation-elevation.md`; raise the
  prompt and type the password there rather than passing it through a command line.
- Treat UI driving as read-only by default. Do not trigger destructive actions such as delete, approve or
  submit unless the user asked for that flow.
- Always close the app when finished so no orphan process is left holding database connections.
- Prefer a build plus a focused test for logic. Use UI automation for what only the running app can prove.

## The procedure

Load the skill `.github/skills/winui3-ui-automation/SKILL.md`. Its `references/procedure.md` carries
launch, connect, read the page, window placement, navigate, close, the anchors for this app, and the nine
roadblocks that stopped driven runs with the fix for each. Helpers live in `scripts/ui-automation/`.
Measurements behind each recipe are in `.github/instructions/records/ui-automation-verification.md`.

## Relationship to XamlMcp

`xamlmcp` is unregistered; the CLI is still installed but not on `PATH`. Why, and when it returns:
`.github/memories/repo/mcp-tooling.md`. Which servers are registered:
`.github/instructions/mcp-doc-research.instructions.md`.
