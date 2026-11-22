---
name: winui3-scaling-audit
description: 'Scan XAML and C# in a WinUI 3 repo for scaling anti-patterns: hardcoded FontSize, numeric fixed Width/Height, non-4-epx margins, literal # colors, and the DisplayInformation.GetForCurrentView migration gotcha. Use when reviewing layout for DPI/resolution independence or before claiming a page scales correctly.'
---

# Audit a repo for scaling anti-patterns

Run the scanner and fix what it reports. It reads the source, not the build, so it runs in
seconds on a whole repo.

```powershell
pwsh -NoProfile -File .github\skills\winui3-scaling-audit\scripts\audit-scaling.ps1 -Path .
```

`-Path` defaults to the current directory. It recurses over `*.xaml` and `*.cs`, skipping
`bin`, `obj`, `.git`, and `.vs`.

## What it flags

| Rule | Why it matters |
| --- | --- |
| Hardcoded `FontSize` | Breaks the type ramp and OS text scaling. |
| Numeric fixed `Width` / `Height` | Breaks reflow and per-monitor DPI. |
| Margin/Padding not on the 4 epx grid | Fractional pixels at 125/150% scaling. |
| Spacing/RowSpacing/ColumnSpacing not on the 4 epx grid | Same fractional-pixel problem, and a gap the margin check alone misses. |
| Literal `#` color in XAML | Breaks light/dark and high contrast. |
| `DisplayInformation.GetForCurrentView()` | Throws in WinUI 3 desktop — use `XamlRoot.RasterizationScale`. |

## Reading the result

Each hit is `file:line — text`. Treat hits as candidates, not verdicts:

- A numeric `Width` on a fixed glyph/icon column is deliberate furniture — keep it and say why in a
  comment, so the next reviewer does not "fix" it.
- A page or sample that exists to teach `FontSize`, colour, or absolute positioning will trip the
  scan on purpose. Fix the shell, shared controls and services strictly; review sample pages rather
  than blanketing them.

Report the count of hits before and after your change. "It compiles" is not verification for
layout; this scan plus your app's own runtime checks is.
