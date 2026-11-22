# DPI and scaling verification checklist

What to check before calling a UI change done. The lab builds and self-tests automatically, but
`--selftest` runs at whatever size and DPI the machine happens to have — it cannot tell you the app
looks right on someone else's 16-inch laptop at 180%.

Generate the current machine's numbers first:

```powershell
pwsh -NoProfile -File .github\skills\winui3-dpi-checklist\scripts\dpi-report.ps1
```

That prints the system DPI, connected monitors and the plateaus to test. It never changes a setting.

Related: `pwsh -NoProfile -File .github\skills\winui3-scaling-audit\scripts\audit-scaling.ps1 -Path src`
scans XAML and C# for hard-coded sizes, literal colours and the `DisplayInformation` migration trap.

## 1. Scale plateaus

Settings → System → Display → Scale. Check each the machine offers; tick only what you have seen.

- [ ] 100%
- [ ] 125%
- [ ] 150%
- [ ] 175%
- [ ] 200%
- [ ] 225%
- [ ] 250%
- [ ] 300%
- [ ] 350%
- [ ] 400%

Signs of a failure: clipped text, a horizontal scrollbar on a page, a pane that stays expanded and
squeezes the content, or blurry text (which means the app is not per-monitor DPI aware — in WinUI 3
that only happens if something is not using effective pixels).

## 2. Window sizes

The shell uses `PaneDisplayMode="Auto"`, `OpenPaneLength="272"`,
`CompactModeThresholdWidth="640"`, `ExpandedModeThresholdWidth="1008"`.

- [ ] Wider than 1008 epx — pane expanded at 272, content capped at 1000 and centred.
- [ ] 641–1007 epx — pane collapses to the 48 epx icon rail; content keeps the full remaining width.
- [ ] 640 epx and below — menu button only; opening the pane overlays the content.
- [ ] Cannot be dragged smaller than 500 × 600 epx (`WM_GETMINMAXINFO` floor).
- [ ] The window opens at 1360 × 900 **effective** pixels, not 1360 × 900 physical.

## 3. Moving between monitors

- [ ] Drag from a 100% display to a 150% display: text stays sharp, nothing clips, the minimum size
      is still 500 × 600 epx on the new display.
- [ ] Drag back: the layout settles with no stuck size and no blur.
- [ ] Change the display scale while the app is running: it adjusts immediately, no restart.

## 4. Theme and accessibility

- [ ] Toggle light/dark while running — colours follow immediately.
- [ ] Turn transparency effects off — Mica falls back to a solid colour.
- [ ] High contrast on — text and controls remain legible.
- [ ] Windows "Make text bigger" raised — app text grows.
- [ ] "Turn off animations" on — layout changes become instant.
- [ ] Tab forward and Shift+Tab back — order follows the on-screen order.

## 5. Text at narrow widths

- [ ] Long part numbers keep both ends (`100...0-A`) and reveal the full value on hover.
- [ ] Short labels shrink rather than clip, and stop at the 8 epx floor.
- [ ] Body text wraps; it is never shrunk.
- [ ] A data table scrolls sideways only when its columns genuinely do not fit.

## Sign-off

Report the pass/fail count and name any plateau you could not test. "It compiles" and "the self-test
passed" are not scaling verification — they do not change the display scaling.
