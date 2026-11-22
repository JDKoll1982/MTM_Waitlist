---
name: winui3-text-fit
description: Add shrink-to-fit labels and middle-ellipsis text to a WinUI 3 app. Use when button or field labels clip, or when long identifiers such as part numbers must keep their start and end visible.
---

# Shrink-to-fit and middle ellipsis

WinUI 3 has **no** native shrink-to-fit text and **no** middle ellipsis (`TextTrimming` only
ellipsizes the end). Three files, split so the arithmetic is testable without a XAML host:

- [TextEllipsis.cs](./references/TextEllipsis.cs) — `TextEllipsis.Middle(text, maxChars)` is pure
  C#; turns "MTM-4821-BRKT-ASSY" into "MTM-...ASSY" (start and end stay visible).
- [ShrinkToFit.cs](./references/ShrinkToFit.cs) — pure math: `NeedsShrinking(...)` and
  `NextFontSize(measured, available, current, floor)`. Proportional estimate, at least half a point
  down per step so a caller loop always terminates.
- [TextFit.cs](./references/TextFit.cs) — the WinUI half: `textBlock.ShrinkToWidth(available)`.

## Why the split

The rule that goes wrong silently is the arithmetic, not the `Measure` call around it. Keeping
`TextEllipsis` and `ShrinkToFit` free of WinUI types means they compile and unit-test in a plain
`net10.0` console test project — no XAML host, no app boot.

## When to use which

| Situation | Use |
| --- | --- |
| Part number, bin code, long identifier in a fixed column | `TextEllipsis.Middle` |
| Button or short label that must stay on one line | `textBlock.ShrinkToWidth(available)` |
| Body text that can reflow | wrap, don't shrink (see `winui3-scaling-policy`) |
| Hover reveals full text | `IsTextTrimmed` + a tooltip converter, not a helper here |

## Rules

- Shrink only for short labels and buttons; never shrink body text.
- Stop shrinking at a floor (default 8 epx) — a smaller font is worse than wrapping.
- Pair any trimmed identifier with a tooltip showing the full value.
- Measure with infinite width first: `DesiredSize` reflects the space you offered, so offering the
  slot width hides the overflow you are trying to detect.

## Testing

Put `TextEllipsis` and `ShrinkToFit` in a WinUI-free assembly and test them there. Cases worth
keeping: both ends survive a trim; the result never exceeds its budget; an already-fitting or
at-floor label is returned unchanged; a 0.3% overflow still steps down (otherwise the caller's loop
never terminates); and a simulated measure loop settles for a range of widths.
