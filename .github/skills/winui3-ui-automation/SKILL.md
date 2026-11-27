---
name: winui3-ui-automation
description: Drive and inspect the running MTM_Waitlist window with Windows UI Automation from PowerShell, to verify startup routing, shell navigation and window state that a build or unit test cannot prove. Use when a change's observable outcome is which page appears or what the window looks like, or when a driven run stops part-way.
---

# WinUI 3 UI Automation

The running app exposes a full UI Automation tree, and Windows PowerShell already ships the
`UIAutomationClient` and `UIAutomationTypes` assemblies. No install, package or code change is needed.

**Read [references/procedure.md](references/procedure.md)** for the recipe: launch, connect to the
window, read the page, read window placement, navigate, close, plus the anchors for this app, what the
technique catches, its verification log and the nine roadblocks that stopped driven runs.

`scripts/ui-automation/` holds the runnable helpers, and `docs/scripts/scripts-automation-ui-automation.md`
section 11 carries the same recipe in long form.

## Rules

The rules for using this skill live in
`.github/instructions/winui3-ui-automation.instructions.md`, which loads on every XAML, C#, PowerShell and
RESW edit. Read them before driving the app; they cover privilege matching, read-only defaults, and
closing the app afterwards.

## Before you trust a recipe

Read `.github/instructions/records/ui-automation-verification.md`. It records what was exercised, on
which machine, and what it returned on 2026-09-11, 2026-09-20 and 2026-10-19, including the two long-open
items and their resolution. Its 2026-10-19 section is the post-repair re-run: whole-surface walk green at
25 of 29 surfaces with 0 findings.
