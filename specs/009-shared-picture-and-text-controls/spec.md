# Feature 009 — Two shared controls: click-to-enlarge pictures, and one-line auto-scrolling text

> Status: **in progress** (working tree on top of `0b78b2c`, 2026-09-24). Written lightweight rather than through the
> full Spec Kit phase cycle because the request arrived as two concrete UI changes with the shape already decided;
> the requirements, decisions and evidence are recorded here so the work can be reviewed and continued.

## Why

Two things the operator-facing screens get wrong today, both of them the same kind of mistake — a rule that each
surface re-implements for itself:

1. **A picture can only be enlarged on one screen.** The waitlist detail page enlarges its two hero pictures through
   a page-level overlay and three code-behind handlers (`WaitlistViewDetailPage.xaml` :548–568, `.xaml.cs` :46–66).
   Every other screen that draws a picture — the wizard's die / component / dunnage / preview / confirmation steps,
   the Setup part list and review, the part-picture manager, the dunnage-search dialog, the work-centre cards — can
   do nothing with it. Which surfaces enlarge, and how they look while doing it, is decided per screen.
2. **Card titles wrap, then clip.** The shared card's title is a wrapping `TextBlock`. On a narrow card a long value
   ("Cardboard Sheets / Slip Sheets", "Foam / Molded Inserts") wraps to two or three lines and either pushes the
   card out of shape or is cut off, so the reader cannot tell what the card says.

Both are fixed the same way: one control that owns the rule, used everywhere the rule applies.

## What — requirement set

**A. Click to enlarge**

- **A1** A shared control draws a picture and, when clicked, shows that same picture enlarged to fit the window.
- **A2** The enlarged view is dismissed by a click on the backdrop around the picture, by its own close button — an
  **X in the view's top-right corner** — and with `Esc`. The backdrop is semi-transparent and fills the page it
  covers, so the page stays visible behind the picture, dimmed. A click on the picture itself is **not** a dismissal.
- **A3** Every surface that draws a **content picture** uses the control: the picture always comes through the
  existing picture converter, so the one picture rule (real picture, or the one shared placeholder) is unchanged.
  This includes the two work-centre photo cards (`SetupWorkCenterPage`, `NewRequestWorkCenterPage`), which drew
  their photo as an `ImageBrush` on a rounded `Border`'s background — the one exception A3 used to allow itself.
- **A4** Enlarging can be switched off per person, from Settings, and the switch is **on** by default.
- **A5** With the feature off, clicking a picture does nothing — the picture is still drawn, and nothing announces
  that it can be enlarged.
- **A6** A picture that is not a content picture (app icon, empty-state illustration, InfoBar glyph) is not given
  the affordance — enlarging a 16×16 title-bar icon is not a feature.
- **A7** **The enlarging gesture follows the host.** Where the picture stands on its own, a plain click enlarges it.
  Where the picture is part of something that acts on a click — inside a button, or inside a list row that raises
  `ItemClick` or selects on a click (the waitlist cards, the shell's building flyout, the New Request option cards,
  the Setup work-centre and part lists) — a plain click belongs to that host, and **Shift+click** enlarges the
  picture. The tooltip names the gesture that applies, so nothing announces a plain click that would not enlarge.

**B. One-line text that scrolls when it does not fit**

- **B1** A shared control draws text on exactly one line.
- **B2** When the text fits, it is drawn still. When it does not fit, it scrolls across its own field and repeats.
- **B3** If the system has animations turned off, the text is truncated with an ellipsis instead of moving — the user
  who asked for less motion gets less motion, and still sees a readable single line.
- **B4** The scrolling must cost nothing when it is not happening: no timers, no UI-thread per-frame work, and the
  animation stops when the control leaves the tree (these controls live inside virtualized lists).

## Design decisions

| # | Decision | Why |
| --- | --- | --- |
| D1 | Two `UserControl`s in `Module_Shared/Controls/` — `ClickToEnlargeImageView`, `AutoScrollTextView` | The `Module_*` folders are compiled into the app assembly, so both modules and the app itself can use them; these two are used by Waitlist, Setup and Settings alike |
| D2 | The enlarging control owns a `Popup` sized to the page it covers, rather than each page owning an overlay | Makes the control self-contained: a screen gets the behaviour by using the control, and no page needs an `ImageViewerOverlay` any more. A `Popup` measures to its child and is placed where it sits in the layout, so the control gives the backdrop the page's size and offsets it back to the page's own origin — driven and measured in the running app: a 760×500 window gave a backdrop at `0,0` of `744×491`, the client area |
| D3 | The popup's child is the whole view: a semi-transparent backdrop (`#99000000`), the picture, and the X | The popup does not fire routed key events itself, so `Esc` is handled on its child, per the `Popup` class remarks (see Sources). The X is in the corner because that is where a reader looks in something that covers the page, and a bottom panel would need a dialog's chrome to sit in |
| D4 | `Source` is an `ImageSource`, and the consumer keeps its own frame | The frames carry deliberate sizes (72/64/96/120) and themed backgrounds; the control replaces the `Image` inside them, so the thumbnail rules already in place are untouched |
| D5 | Enlarge state comes from a small cached service, not a per-click database read | A click must not wait on MySQL; the value is read once and kept, and the Settings toggle updates the same cache so both stay in step |
| D6 | The preference is stored per **signed-in account** (`scope_type = 'user'`, `scope_key = 'user:<id>'`) | The request was "user based". This follows the person to any machine, the way `admin.users.list_filter` already does; a local-settings value would follow the Windows login instead and be shared by two people on one PC |
| D7 | Absent, unreachable or unreadable preference ⇒ **on** | Matches the declared default and keeps the feature working for a reader whose store cannot be reached |
| D8 | Marquee is implemented dependency-free, with a compositor animation on the text's translation | Context7 was consulted for a toolkit control and there is none (`/communitytoolkit/windows` has no marquee text control — its scrolling offering is `ScrollViewerExtensions`, `WrapPanel`). A compositor animation is the memory-efficient route the request asked for: it is driven by the compositor, not the UI thread, and it is stopped when the control unloads |
| D9 | Text is truncated instead of scrolled when the system reports animations off | Accessibility: honouring the OS motion setting, with a readable fallback rather than a frozen marquee |
| D10 | Light dismiss is **off** on the enlarged view's popup | A light-dismiss popup also closes when the application loses focus, so a reader who alt-tabbed away came back to a view that had closed itself (found by driving the running app, 2026-09-24). The backdrop covers the page, so there is nothing outside it to click |
| D11 | The enlarging gesture is read from the picture's **nearest host**, not declared per surface | One rule for every surface, and the rule cannot be forgotten: the two ways a click can already be spent are a `ButtonBase` around the picture and a list that raises `ItemClick` or selects on a click. The walk stops at the **first** list the picture sits in, so a card list that does not act on a click (the Settings dunnage lists, whose action is their own Hide/Show buttons) keeps the click even though the page around it is built from expanders and lists too. The shared card template is used both by a clickable list (the building flyout) and by one that is not (Settings), so a per-surface declaration would put the wrong gesture on one of them — and the wrong gesture looks identical on screen until somebody clicks |
| D12 | The Shift modifier comes from the pointer event (`PointerRoutedEventArgs.KeyModifiers`) | Reading the keyboard's current state at handling time did not see the Shift of the very click being handled, so the gesture never fired — found in the running app on 2026-09-24, and the reason the modifier is taken from the event |

## Files

| Area | Files |
| --- | --- |
| Controls | `Module_Shared/Controls/ClickToEnlargeImageView.xaml(.cs)`, `Module_Shared/Controls/AutoScrollTextView.xaml(.cs)` |
| Preference | `MTM_Waitlist.Core/Contracts/Services/IPictureEnlargePreference.cs`, `MTM_Waitlist.Settings/Services/PictureEnlargePreference.cs`, `MTM_Waitlist.Settings/Models/ConfigSettingValue.cs`, `MTM_Waitlist.Settings/Services/DependencyInjection/ModuleDependencyInjectionExtensions.cs` |
| Settings screen | `MTM_Waitlist.Settings/ViewModels/SettingsViewModel.cs`, `Module_Settings/Views/SettingsPage.xaml`, `Strings/en-us/Resources.resw` |
| Converted surfaces | the content-picture surfaces listed in the implementation notes |
| Shared card | `Styles/PartPictureCardTemplate.xaml` (title becomes `AutoScrollTextView`) |

## Sources consulted

- **Microsoft Learn — `Popup` class (Windows App SDK 2.0)**, fetched 2026-09-24 because the docs-search MCP tool
  reported disabled in this session (a stale-tool-snapshot issue this repo documents). Two facts taken from it:
  `IsLightDismissEnabled` hides the popup when the user taps away, and *"The Popup doesn't fire RoutedEvents, for
  example KeyDown and PointerPressed. You can wire an event handler for these RoutedEvents on the child of the
  Popup."* — which is why `Esc` is handled on the popup's child.
- **Microsoft Learn — `XamlRoot` class (Windows App SDK 2.0)**, fetched the same day for the sizes the view needs:
  `Size` (the content area, which is what fills the page), `Changed` (re-sizing while the view is open), and
  `RasterizationScale`.
- **Microsoft Learn — `PointerRoutedEventArgs` class (Windows App SDK 2.0)**, fetched the same day after the first
  build's gesture failed in the running app: *"Specific members of PointerRoutedEventArgs, such as `KeyModifiers`"*,
  used to read the modifiers of the click being handled (D12).
- **Context7** — `/communitytoolkit/windows` (no marquee text control; `ScrollViewerExtensions` and `WrapPanel` are
  what it offers), `/microsoft/microsoft-ui-xaml` (storyboard/composition animation of a translation is the
  supported animation primitive). Library docs only; no API was taken on trust from memory.

## Evidence

| Claim | Evidence |
| --- | --- |
| Build | `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false` — **succeeded, 0 Warning(s), 0 Error(s)** |
| Suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` — **1562 total, 0 failed, 1507 passed, 55 skipped** (environment-gated) |
| Every content picture uses the control (A3) | `SharedControlsMarkupTests.EveryContentPicture_IsDrawnByTheSharedControl` scans every `.xaml` in the repository for an `Image` **or `ImageBrush`** element carrying a picture-resolver binding. **Fails first, with teeth**: with `SetupReviewPage.xaml` reverted to its pre-009 markup (`git stash push`), it reports `Expected:<0>. Actual:<1>` naming the file; restored, it passes. The two work-centre photo cards are covered by the same scan |
| The card title is one line drawing the scrolling control (B1, B2) | `SharedControlsMarkupTests.TheSharedCardTitle_IsDrawnByTheOneLineScrollingText` — no `TextBlock` binds the title any more |
| The line cannot wrap | `SharedControlsMarkupTests.TheTextControl_KeepsItsLineOnOneLine` — `MaxLines="1"`, `TextWrapping="NoWrap"`, and a clip on the viewport |
| The control owns the enlarged view (A1, A2) | `SharedControlsMarkupTests.ThePictureControl_OwnsTheEnlargedView` — the popup, its non-light-dismiss flag, the semi-transparent backdrop with its `Tapped` and `KeyDown` handlers, and the X glyph in the top-right corner are all the control's; the Shift rule is asserted here too (the press handler, `ButtonBase`, `IsItemClickEnabled`, `VirtualKeyModifiers.Shift`, and the `Shared_ClickToEnlarge.ShiftTooltip` wording) |
| The default is on, per person, and cannot break a picture (A4, A5, D6, D7) | `PictureEnlargePreferenceTests` — 6 cases: default on when nothing is stored; a stored answer wins; an unreachable store keeps the default; a write lands at `user:<id>` with the person's id and the `bool` type; a refused write still applies for the session; a read is still owed once somebody signs in |
| The previous markup tests still hold | The 12 tests that looked for a bare `Image` now look for the control and pass unchanged in intent (FR-018/FR-019 checks) |
| **The scroll and the enlarged view, in the running app** | **Verified 2026-09-24** on the built `Debug` app: opened Settings → Operations → *Dunnage Type Visibility*, captured the window twice 1.3 s apart, and compared pixels inside each card title. `Cardboard Sheets / Slip Sheets` (the long one) changed **81 of 220** sampled pixels — it is moving. `Corrugated Boxes`, `Foam / Molded Inserts` and `Gaylords / Bulk Bins` changed **0** — they fit, so they stay still, which is requirement B2's other half. The app was closed afterwards; no process was left running. |
| **The gesture, and the new enlarged view, in the running app (A2, A7)** | **Driven 2026-09-24** on the built `Debug` app through UI Automation and real mouse/keyboard input, one check per line, all passing: a plain click on a waitlist card's picture opened the request and left the enlarged view closed; a plain click on the request page's own picture (nothing claims that click) still opened the enlarged view; the X in the top-right corner closed it; **Shift+click on a waitlist card's picture opened the enlarged view and left the page on the waitlist**; `Esc` closed it; a plain click on a building card's picture in the shell's facility flyout **chose the building** (`Expo Drive` → `Vits Drive`) with the enlarged view closed; **Shift+click on the same card's picture opened the enlarged view and left the chosen building unchanged**; a click on the backdrop closed it; and **a plain click on a Settings dunnage card's picture still opened the enlarged view**, which is the case a first version of the rule got wrong (defect 6). Measured while open: a backdrop of `744×491` at `0,0` in a `744×492` client area — it fills the page — with the X `40×40` at 16 px from the top-right corner. |
| **The work-centre photo cards draw through the control (A3)** | Both cards were converted; a screen capture of *Work Center Setup* shows the photo drawn inside its frame by the control, and the same page's cards still choose the work centre on a plain click. The picture keeps the card's rounded frame but is not itself clipped to that radius — the same look every converted picture in the app already has (`PartPictureCardTemplate`'s picture sits square inside a rounded frame too). |

## Defects the verification found (all fixed here)

None of these could be caught by the test suite, which is why the running app was driven.

1. **Opening the Settings page terminated the app.** The new Appearance card carried `x:Load`, following this
   repository's convention for a conditionally shown element. But an element inside a `SettingsExpander`'s item
   collection is **not in the page's namescope**, and the generated `x:Load` binding resolves the element with
   `FindName`, which answered null and took the process down. Event log, `.NET Runtime`, 2026-09-24 06:37:
   `Update_ViewModel_IsEnlargePicturesPanelVisible` → `FrameworkElement.FindName` → *"The process was terminated due
   to an unhandled exception."* **Fix:** the card is hidden with `Visibility` (a property binding, which needs no
   name lookup) instead of `x:Load`. **The convention as written is unsafe inside `SettingsExpander.Items`.**
2. **Expanding the dunnage card lists terminated the app with no exception anywhere** — no event-log entry, no log
   line, the window simply vanished. This was the marquee control deciding inside a layout callback: `SizeChanged`
   and `EffectiveViewportChanged` ran a decision that changed the line's width during the arrange pass, and the
   decision guard was cleared by `StopScrolling`, so the next callback decided again. **Fix:** the decision is
   queued to run after the layout pass (`DispatcherQueue.TryEnqueue`) and the guard is only ever set by a decision,
   never cleared by stopping the animation — a stop can no longer re-arm the callback that caused the stop.
3. **The Shift gesture never fired, and the app looked right while it did not.** With the gesture read from the
   keyboard's current state (`InputKeyboardSource.GetKeyStateForCurrentThread`, the pattern the developer-tooltip
   inspector already uses for Ctrl+Alt), a Shift+click on a waitlist card opened the request and left the picture
   alone: the *card* still acted, so nothing looked broken — only the check's per-step output said
   `viewer=0`. **Fix:** the modifier now comes from the pointer event itself (`PointerRoutedEventArgs.KeyModifiers`),
   which is the modifiers of the click being handled rather than whatever the keyboard says a moment later. D12.
4. **The enlarged view closed itself when the application lost focus.** With `IsLightDismissEnabled="True"` the
   view also closed when the app was deactivated, so alt-tabbing away and back lost the picture — a route out that
   was never asked for, and one the dialog it replaced did not have (also found only by driving the app). **Fix:**
   light dismiss is off; the backdrop, the X and `Esc` are the ways out. D10.
5. **Revealed, not caused: a popup is placed where it sits in the layout.** The first build put the backdrop beside
   the thumbnail it was opened from rather than over the page — measured as a `1920×1032` backdrop starting at
   `374,274`. **Fix:** the control offsets the popup back to the page's origin (`TransformToVisual` from the popup's
   own host), which is what makes the view cover the page on any surface. D2.
6. **The Settings dunnage card pictures stopped enlarging on a plain click.** The first version of the gesture rule
   kept walking outward until it found *any* claiming list, and the Settings page is itself built from expanders and
   lists, so a picture in a card list that does not act on a click was treated as claimed and its plain click did
   nothing — the same symptom the feature exists to prevent, one layer further out. **Found by driving the app**: a
   Shift+click at one point opened the view and a plain click at the same point did not. **Fix:** the walk stops at
   the first list the picture sits in, and that list alone decides. D11.

## Deliberate omissions (review these first)

1. **The two work-centre photo cards are converted now (supersedes the earlier omission).** `SetupWorkCenterPage.xaml`
   and `NewRequestWorkCenterPage.xaml` used to draw their photo as an `ImageBrush` on a rounded `Border`'s
   background, and were left alone because converting might square the frame's corners. They are converted: the
   picture is drawn by the control inside the same frame, and the frame keeps its rounding while the picture inside
   it is not clipped to that radius — the same look every converted picture in the app already has. Verified on
   screen (see Evidence). **Decide:** accept the square corner inside the rounded frame, or give the control a
   corner radius so the picture matches its frame exactly.
2. **The enlarged view is bounded by the page, not stretched to fill it.** A picture smaller than the page is drawn
   at its own size and a larger one is fitted (minus a 64-pixel edge that keeps the backdrop clickable and the X
   clear of the picture); a small picture blown up to fill the screen was rejected as a worse picture.
3. **The backdrop is a fixed 60% black.** It is not a theme resource, so it does not follow the app's theme; a
   light-theme page behind it is dimmed the same way a dark one is.
4. **The scroll is an animation, so it is stopped when the system reports animations off.** The fallback is a
   single truncated line, not a frozen marquee.
5. **No test covers the scrolling motion itself** — it is a composition animation on a rendered element, and the
   suite does not render. What is guarded is the markup contract (`MaxLines`, `NoWrap`, the clip), the fact that the
   card uses the control, and — from this session — the observed behaviour in the app.
6. **The gesture rule is asserted as code, not exercised as behaviour, by the suite.** The ancestor walk needs a
   visual tree, so the suite checks that the rule is present (the press handler, `ButtonBase`, `IsItemClickEnabled`,
   `VirtualKeyModifiers.Shift`) and the behaviour was driven in the running app instead — including the case the
   rule exists to protect: a clickable list's plain click, and a non-clickable list's plain click, both landing
   where they should.
7. **The other wrapping text is inventoried, not converted.** `SCROLLING-TEXT-CANDIDATES.md` in the repository root
   lists 51 data-bound elements that can reach a second row (plus the possible and not-candidate sets) with file,
   line and automation name, for a decision on what else should scroll.
