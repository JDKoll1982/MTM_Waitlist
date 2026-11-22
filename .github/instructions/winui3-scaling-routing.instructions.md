---
name: 'WinUI 3 scaling & layout — skill routing'
description: 'Route WinUI 3 scaling/DPI/layout work to the right skill in .github/skills, and recall the non-negotiable layout rules. Use when writing or reviewing XAML or windowing code: sizing, DPI, text overflow, tables, or responsive behavior.'
applyTo: '**/*.{xaml,cs}'
---

# WinUI 3 scaling & layout — skill routing

## Non-negotiables (apply always)

- Design in **effective pixels**; no hard-coded `Width`/`Height` on layout.
- Spacing on the **4 epx grid**; `{ThemeResource}` brushes only; type-ramp styles, no raw `FontSize`.
- Content **reflows** on resize — never whole-window zoom.
- Per-monitor DPI aware, always sharp (WinUI 3 default if you don't pin sizes).
- Honor OS accessibility: text scaling, high contrast, reduce-motion, standard touch sizing.

## Which skill to use, and when

| Situation | Skill |
| --- | --- |
| Any layout/sizing/DPI rule, or before writing page XAML | `winui3-scaling-policy` — the full decision rules |
| Reviewing layout for DPI/resolution bugs, or "does this page scale?" | `winui3-scaling-audit` — scan XAML/C# for anti-patterns |
| Testing scaling before release, or "blurry/clipped on one monitor" | `winui3-dpi-checklist` — DPI report + manual checklist |
| Window min size, fullscreen, persistence, snap width, title bar | `winui3-windowing` — windowing rules |
| Clipped labels or long identifiers (part numbers, bins) | `winui3-text-fit` — shrink-to-fit + middle ellipsis |
| Tabular data that clips columns on narrow windows | `winui3-adaptive-table` — shared column-aware table |

## Work order

1. Read `winui3-scaling-policy` first.
2. Run `winui3-scaling-audit` to find violations, then fix them.
3. Run `winui3-dpi-checklist` before claiming scaling is done.
4. Reach for the feature skills (`windowing`, `text-fit`, `adaptive-table`) only for those features.

## Where the skills live

In this repo: `.github/skills/winui3-*`. The audit and DPI scripts run with:

```powershell
pwsh -NoProfile -File .github\skills\winui3-scaling-audit\scripts\audit-scaling.ps1 -Path .
pwsh -NoProfile -File .github\skills\winui3-dpi-checklist\scripts\dpi-report.ps1
```

Full decision rules: `.github/skills/winui3-scaling-policy/references/rules.md`.
Manual release checklist: `docs/dpi-checklist.md`.

## Relationship with `winui3-adoption.instructions.md`

Both files touch spacing, the type ramp and the 640/1008 breakpoints. They agree; use them for
different jobs:

| Job | Use |
| --- | --- |
| One-off restyle of an existing screen onto the design system (measure, convert, ship) | `winui3-adoption.instructions.md` |
| Ongoing rules while writing or reviewing layout: sizing, DPI, windowing, overflow, tables | the `winui3-scaling-*` skills |
| Finding violations across the repo | `winui3-scaling-audit` |

If the two ever disagree, the values in `winui3-adoption.instructions.md` win for this repo,
because they were measured against its actual screens; update the skill instead of drifting.
