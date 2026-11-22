# DPI verification checklist

Run the reporter first (`scripts/dpi-report.ps1`), then perform these by hand. Check each box
only after you have actually seen the app at that setting.

## Scale plateaus

Test the app at each plateau. `settings > System > Display > Scale`.

- [ ] 100%
- [ ] 125%
- [ ] 150%
- [ ] 175%
- [ ] 200%
- [ ] 225% (if offered)
- [ ] 250% (if offered)
- [ ] 300% (if offered)
- [ ] 350% (if offered)
- [ ] 400% (if offered)

## Per-monitor drag

- [ ] Drag the window from a 100% monitor to a 150% monitor — text stays sharp, nothing clips.
- [ ] Drag it back — layout settles, no blur, no stuck size.
- [ ] Resize to the minimum usable width (500 epx) — nav pane collapses to the icon rail,
      content reflows, no horizontal scrollbar (except a data table).

## Live changes

- [ ] Change the display scale while the app is running — the app adjusts immediately, no restart.
- [ ] Toggle light/dark mode while running — colors follow, no restart.
- [ ] Toggle "transparency effects" off — Mica/Acrylic fall back to a solid color.
- [ ] Turn on high contrast — text and controls stay legible.

## Text and accessibility

- [ ] Raise Windows "Make text bigger" (100% → 225%) — app text grows accordingly.
- [ ] Turn on "turn off animations" — layout changes become instant, nothing animates.
- [ ] Tab through the page and Shift+Tab back — order follows the on-screen order.

## Honest gaps to verify (no fake behavior)

- [ ] Minimum window size works (via WM_GETMINMAXINFO), and a smaller resize is refused.
- [ ] Fullscreen enters and exits cleanly (AppWindow FullScreenPresenter).
- [ ] Long part numbers show middle ellipsis and reveal full text on hover.
- [ ] Buttons/labels that shrink to fit never clip at 200%+.

## Sign-off

Report the pass/fail count. A pass is a setting where the app looked correct; anything less is
a finding to fix before shipping.
