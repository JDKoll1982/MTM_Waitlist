---
name: winui3-scaling-policy
description: The distilled WinUI 3 scaling, DPI, and responsive-layout rules for designing or reviewing any WinUI 3 app. Use when writing or reviewing XAML layout, sizing, windowing, DPI, text overflow, or responsive behavior so the result is resolution- and monitor-independent.
---

# WinUI 3 scaling & layout policy

Apply these rules when writing or reviewing any WinUI 3 XAML or windowing code. The full
decision rules are in [rules.md](./references/rules.md). Load that file when you need the
complete list; this summary is the non-negotiables.

## Non-negotiables

- Use **effective pixels**, never physical pixels. No hard-coded `Width`/`Height` on layout.
- Spacing on the **4 epx grid**: 4 / 8 / 12 / 16 / 24 / 32 / 40 / 48 only.
- Theme via `{ThemeResource}` only — zero literal `#` colors.
- Type via the ramp (`BodyTextBlockStyle`, `TitleTextBlockStyle`, …) — never raw `FontSize`.
- Content **reflows** when the window resizes; never whole-window zoom.
- Per-monitor DPI aware, always sharp. WinUI 3 does this automatically if you don't pin sizes.
- Honor OS accessibility: text scaling, high contrast, reduce-motion, standard touch sizing.

## Where the trap is

WinUI 3 has **no native** minimum window size, no shrink-to-fit text, and no middle ellipsis.
These are taught, not faked — see [rules.md](./references/rules.md) for the correct workarounds.

## Verify before claiming done

```powershell
pwsh -NoProfile -File .github\skills\winui3-scaling-audit\scripts\audit-scaling.ps1
pwsh -NoProfile -File .github\skills\winui3-dpi-checklist\scripts\dpi-report.ps1
```

The first scans XAML for anti-patterns; the second reports the current display scale and the
manual test checklist.
