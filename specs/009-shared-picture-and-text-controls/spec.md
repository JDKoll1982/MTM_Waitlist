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
- **A2** The enlarged view is dismissed with `Esc` and with its own close button. A click on the dimmed area behind
  it is **not** a dismissal — a dialog does not light-dismiss, so nothing here claims it does.
- **A3** Every surface that draws a **content picture** uses the control: the picture always comes through the
  existing picture converter, so the one picture rule (real picture, or the one shared placeholder) is unchanged.
- **A4** Enlarging can be switched off per person, from Settings, and the switch is **on** by default.
- **A5** With the feature off, clicking a picture does nothing — the picture is still drawn, and nothing announces
  that it can be enlarged.
- **A6** A picture that is not a content picture (app icon, empty-state illustration, InfoBar glyph) is not given
  the affordance — enlarging a 16×16 title-bar icon is not a feature.

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
| D2 | The enlarging control owns a `Popup` sized to the window, rather than each page owning an overlay | Makes the control self-contained: a screen gets the behaviour by using the control, and no page needs an `ImageViewerOverlay` any more |
| D3 | `Popup` with its own dark scrim child; close on scrim tap / button / `Esc` | Reproduces the look the detail page already has, and the Popup's own key events do not fire — the handler goes on the child, per the Popup class remarks (see Sources) |
| D4 | `Source` is an `ImageSource`, and the consumer keeps its own frame | The frames carry deliberate sizes (72/64/96/120) and themed backgrounds; the control replaces the `Image` inside them, so the thumbnail rules already in place are untouched |
| D5 | Enlarge state comes from a small cached service, not a per-click database read | A click must not wait on MySQL; the value is read once and kept, and the Settings toggle updates the same cache so both stay in step |
| D6 | The preference is stored per **signed-in account** (`scope_type = 'user'`, `scope_key = 'user:<id>'`) | The request was "user based". This follows the person to any machine, the way `admin.users.list_filter` already does; a local-settings value would follow the Windows login instead and be shared by two people on one PC |
| D7 | Absent, unreachable or unreadable preference ⇒ **on** | Matches the declared default and keeps the feature working for a reader whose store cannot be reached |
| D8 | Marquee is implemented dependency-free, with a compositor animation on the text's translation | Context7 was consulted for a toolkit control and there is none (`/communitytoolkit/windows` has no marquee text control — its scrolling offering is `ScrollViewerExtensions`, `WrapPanel`). A compositor animation is the memory-efficient route the request asked for: it is driven by the compositor, not the UI thread, and it is stopped when the control unloads |
| D9 | Text is truncated instead of scrolled when the system reports animations off | Accessibility: honouring the OS motion setting, with a readable fallback rather than a frozen marquee |

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
- **Context7** — `/communitytoolkit/windows` (no marquee text control; `ScrollViewerExtensions` and `WrapPanel` are
  what it offers), `/microsoft/microsoft-ui-xaml` (storyboard/composition animation of a translation is the
  supported animation primitive). Library docs only; no API was taken on trust from memory.

## Evidence

| Claim | Evidence |
| --- | --- |
| Build | `dotnet build MTM_Waitlist.sln -p:Configuration=Debug -p:Platform=x64 /m:1 /nodeReuse:false` — **succeeded, 0 Warning(s), 0 Error(s)** |
| Suite | `dotnet test MTM_Waitlist.Tests/MTM_Waitlist.Tests.csproj -c Debug -p:Platform=x64` — **1562 total, 0 failed, 1507 passed, 55 skipped** (environment-gated) |
| Every content picture uses the control (A3) | `SharedControlsMarkupTests.EveryContentPicture_IsDrawnByTheSharedControl` scans every `.xaml` in the repository for an `Image` element carrying a picture-resolver binding. **Fails first, with teeth**: with `SetupReviewPage.xaml` reverted to its pre-009 markup (`git stash push`), it reports `Expected:<0>. Actual:<1>` naming the file; restored, it passes. |
| The card title is one line drawing the scrolling control (B1, B2) | `SharedControlsMarkupTests.TheSharedCardTitle_IsDrawnByTheOneLineScrollingText` — no `TextBlock` binds the title any more |
| The line cannot wrap | `SharedControlsMarkupTests.TheTextControl_KeepsItsLineOnOneLine` — `MaxLines="1"`, `TextWrapping="NoWrap"`, and a clip on the viewport |
| The control owns the enlarged view (A1) | `SharedControlsMarkupTests.ThePictureControl_OwnsTheEnlargedView` — the dialog and the full-size flag are the control's, and the close wording ships |
| The default is on, per person, and cannot break a picture (A4, A5, D6, D7) | `PictureEnlargePreferenceTests` — 6 cases: default on when nothing is stored; a stored answer wins; an unreachable store keeps the default; a write lands at `user:<id>` with the person's id and the `bool` type; a refused write still applies for the session; a read is still owed once somebody signs in |
| The previous markup tests still hold | The 12 tests that looked for a bare `Image` now look for the control and pass unchanged in intent (FR-018/FR-019 checks) |
| **The scroll and the enlarged view, in the running app** | **Verified 2026-09-24** on the built `Debug` app: opened Settings → Operations → *Dunnage Type Visibility*, captured the window twice 1.3 s apart, and compared pixels inside each card title. `Cardboard Sheets / Slip Sheets` (the long one) changed **81 of 220** sampled pixels — it is moving. `Corrugated Boxes`, `Foam / Molded Inserts` and `Gaylords / Bulk Bins` changed **0** — they fit, so they stay still, which is requirement B2's other half. Clicking a card's picture opened the enlarged dialog (a `CloseButton` appeared, and the screenshot shows the picture enlarged with the page dimmed). The app was closed afterwards; no process was left running. |

## Defects the verification found (both fixed here)

Neither of these could be caught by the test suite, which is why the running app was driven.

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

## Deliberate omissions (review these first)

1. **Two work-centre photo cards are not converted.** `SetupWorkCenterPage.xaml` and `NewRequestWorkCenterPage.xaml`
   draw their photo as an `ImageBrush` **on a rounded `Border`'s background**, not as an `<Image>` element. The
   rounding comes from the border's own background, so replacing it with a child control would square off the
   corners unless the control clipped to a matching radius — a visual change that cannot be checked without running
   the app. They are therefore left as they were, and they are the one exception the A3 audit allows itself to
   ignore (it only looks at `Image` elements). **Decide:** accept, or convert them once the corner treatment is
   confirmed on screen.
2. **The enlarged view is bounded by the window, not the whole window.** It is a dialog whose content is the
   picture; the picture is measured at its natural size and capped to the window (minus a margin), so a small
   picture is shown at its own size and a large one is fitted rather than clipped. A page-wide overlay showing a
   small picture blown up to fill the screen was rejected as a worse picture.
3. **A backdrop click does not dismiss it.** The dialog dims the page and closes on `Esc` or its own Close button.
4. **The scroll is an animation, so it is stopped when the system reports animations off.** The fallback is a
   single truncated line, not a frozen marquee.
5. **No test covers the scrolling motion itself** — it is a composition animation on a rendered element, and the
   suite does not render. What is guarded is the markup contract (`MaxLines`, `NoWrap`, the clip), the fact that the
   card uses the control, and — from this session — the observed behaviour in the app.
6. **The other wrapping text is inventoried, not converted.** `SCROLLING-TEXT-CANDIDATES.md` in the repository root
   lists 51 data-bound elements that can reach a second row (plus the possible and not-candidate sets) with file,
   line and automation name, for a decision on what else should scroll.
