---
name: winui3-dpi-checklist
description: Report the current display scale and print the manual DPI verification checklist for a WinUI 3 app. Use when testing scaling before a release, after changing layout, or when a user reports blurry or clipped UI on a specific monitor.
---

# DPI report and test checklist

Run the reporter first, then walk the checklist by hand. The script only reads the machine
state — it never changes display settings.

```powershell
pwsh -NoProfile -File .github\skills\winui3-dpi-checklist\scripts\dpi-report.ps1
```

The full manual checklist is in [checklist.md](./references/checklist.md). Copy it to the repo's own
`docs/` folder so it can be run at release time; this repo publishes it at `docs/dpi-checklist.md`.

## Why

WinUI 3 scales automatically, but three things still fail in practice: hard-coded pixel sizes,
an app that is not per-monitor DPI aware, and forgetting to test the scale plateaus. This skill
makes those failures visible before the user does.

## What the script prints

- Current system DPI and the equivalent scale percent.
- Connected monitors and their reported resolutions.
- Best-effort per-monitor DPI values.
- The plateau list to test (100–400%).
