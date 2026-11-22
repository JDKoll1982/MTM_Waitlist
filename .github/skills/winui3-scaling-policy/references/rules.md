# WinUI 3 scaling & layout — full rules

The complete decision rules, grouped by topic. Each rule is imperative and transferable to any
WinUI 3 / Windows App SDK repo. Distilled from a 47-decision, 13-category review.

## 1. Window & resize

- Reflow on resize — content re-wraps and repositions; never whole-window zoom.
- Remember window size and position between sessions (each window separately).
- Enforce a sensible minimum window size via `WM_GETMINMAXINFO` (WinUI 3 has no native min/max).
- Offer fullscreen with `AppWindow.SetPresenter(FullScreenPresenter)`.
- Apply screen-setting changes live; do not require a restart.
- Minimum usable width: **500 epx** (supports Windows 11 snap layouts).
- No special portrait handling — desktop/landscape only.
- Title bar: document both styles — standard system title bar for main/settings windows, custom
  edge-to-edge for modal/focused windows — and state when to use each.

## 2. Zoom & density

- No in-app "make everything bigger" control — rely on Windows' own display-size setting.
- Text does not grow with the window; a bigger window shows more content.
- Standard spacing; do not ship a compact/dense mode unless explicitly requested.

## 3. Monitors & DPI

- Per-monitor DPI aware, always sharp and right-sized.
- Avoid hard-coded pixel sizes so DPI changes don't clip or blur.
- Live `WM_DPICHANGED` handling is automatic in WinUI 3 — no extra code.

## 4. Accessibility & OS integration

- Honor Windows "Make text bigger" (`TextScaleFactor`, 100–225%).
- Fully adapt colors to high-contrast / color-filter modes (`{ThemeResource}` gives this free).
- Honor "turn off animations" (`UISettings.AnimationsEnabled`).
- Standard control sizing for touch (WinUI controls handle touch/mouse/pen/pen by default).
- Keyboard tab order follows the on-screen order automatically (declaration order), with
  Shift+Tab working for reverse — no hand-numbered `TabIndex`.

## 5. Text & typography

- Headings/titles wrap to a second line when too long.
- Buttons and short labels shrink to fit — no native API; use a measure loop that reduces
  `FontSize` until it fits, or a `Viewbox`.
- Part numbers / identifiers ellipsize in the middle ("MTM-48...ASSY") — `TextTrimming` only
  trims the end, so this needs a custom helper.
- Hover reveals full text only when it is actually cut off (`IsTextTrimmed` + a visibility
  converter for the tooltip).
- Long descriptions in lists/cards cap at ~2 lines with "…"; full text on reading pages.
- Controls size to their content so translated text can grow ~30% (German/Finnish) without
  breaking the layout.

## 6. Tables & data

- Use one shared adaptive table control: scrolls sideways only when it needs to, spans full
  width when it fits, and lets the user show/hide columns. (The single sanctioned exception to
  "no horizontal scrollbar".)

## 7. Navigation & layout

- Left nav pane collapses to a slim icon bar on narrow windows (`NavigationView` Auto mode).
- List + details shows one pane at a time with a back button when narrow.
- Dialog action buttons right-aligned; form labels to the left of the field; side-by-side cards
  equal height; extra vertical space top-aligned (centered only for empty states/dialogs);
  input fields stretch to fill width.

### The `PaneDisplayMode="Left"` trap

Setting `PaneDisplayMode="Left"` pins the pane in its **fully open state at every width** and makes
`CompactModeThresholdWidth` / `ExpandedModeThresholdWidth` dead properties. A narrow window then gets
a full-width pane squeezing the content into a sliver instead of collapsing to the icon rail.

Per the `NavigationViewPaneDisplayMode` enum: `Auto` "changes between minimal, compact, and full
states depending on the width of the window"; `Left` is "shown on the left side of the control in its
fully open state". Use `Auto` and set the thresholds:

```xml
<NavigationView PaneDisplayMode="Auto"
                OpenPaneLength="272"
                CompactModeThresholdWidth="640"
                ExpandedModeThresholdWidth="1008">
```

- ≥ 1008 epx — expanded pane (272).
- 641–1007 epx — 48 epx icon rail; `PaneHeader` (and any search box in it) is hidden, so keep the
  hamburger visible to open the pane as an overlay.
- ≤ 640 epx — menu button only; the pane overlays content when opened.

## 8. Assets

- App icons and logos ship at multiple sizes (16–256 px); in-page images stay standard quality.
- Image fit, both patterns:
  - Type/identity cards: `Stretch="Uniform"` — whole image, letterbox, no crop.
  - Small thumbnails: `Stretch="UniformToFill"` — crop to fill, with a tap-to-view-full affordance.

## 9. Motion

- Layout re-arrangement animates smoothly and subtly.
- Honor the OS reduce-motion setting.

## 10. Lists, scrolling & performance

- Live-updating lists stay anchored — the visible view does not jump when rows arrive above it
  (`IScrollAnchorProvider`).
- Long lists virtualize — load only what's visible, lazy-load on scroll; demonstrate both the
  virtualized (fast) and non-virtualized (slow) panels.

## 11. Globalization, diagnostics & testing

- Mirror the layout for RTL languages (`FlowDirection` + symmetrical margins/padding).
- Teach the migration gotcha: `DisplayInformation.GetForCurrentView()` throws in WinUI 3 desktop
  (no `CoreWindow`); use `XamlRoot.RasterizationScale` instead.
- Document a written DPI test checklist: change display scale live, drag between monitors, and
  check the plateaus (100/125/150/175/200/225/250/300/350/400%).

## 12. Toolbars, flyouts & theme

- Toolbar overflow: lower-priority buttons move into the "…" menu automatically as width shrinks
  (`CommandBar.IsDynamicOverflowEnabled`, default true).
- Flyouts/context menus auto-reposition to stay fully visible near screen edges.
- Follow Windows' light/dark switch immediately at runtime, no restart.
- Window material: Mica with automatic solid-color fallback on Windows 10 / no-transparency /
  weak GPU.

## 13. Dialogs & forms

- Tall dialogs cap their height and scroll the content inside; never grow past the screen
  (`ContentDialogMaxHeight` / `ContentDialogMaxWidth` resources).
- Long forms scroll as one page; a step wizard is only for distinct phases — document both.

## Universal rules

- Effective pixels, not physical pixels. Design for the window, not the screen.
- 4-epx grid for every dimension, margin, and padding.
- `{ThemeResource}` brushes only; zero literal colors.
- Type ramp styles only; zero raw `FontSize` in pages.
- Never invent an API; reflect over the shipped assembly or check Microsoft Learn.
- Never add a dependency to work around a gap — explain the gap and show the supported alternative.
