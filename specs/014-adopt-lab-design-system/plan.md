# Implementation Plan: One set of rules for every screen, window and dialog

**Feature**: `014-adopt-lab-design-system`  
**Spec**: [spec.md](./spec.md)  
**Created**: 2026-10-25

## Scale note

This change spans 77 XAML files across six UI projects, plus 14 screens that raise a dialog from code and the one
separate service executable. A reader should watch two things: that the shared message set is adopted everywhere and
nowhere bypassed, and that the behaviour of each converted flow is unchanged. The three source audits in Phase 1 are
what keep both true after the session ends.

## Summary

Every XAML surface in the repository is brought onto one set of layout, sizing, automation and platform rules. The
route is to build the shared pieces first (dialog sizing resources, a windowed dialog host, and one standard set of
six message dialogs), then convert each raise site to them, then sweep the remaining surfaces for layout, tooltip and
automation conformance, and finally add source audits that fail the build if any of it drifts back.

Three technical choices carry the work. Dialog sizing becomes shared resources rather than per-dialog literals. The
message set is built from `ContentDialog` content shown through `DevWinUI.WindowedContentDialog`, which satisfies both
the request and the api rule that asks for `ContentDialog`. The set lives in `MTM_Waitlist.Shared` so the operator
application and the service console use one implementation, not two.

A tooling gap was recorded here: `plan-blocks.py` cannot print its templates because `call-paths.md`, `code-pins.md`,
`states.md` and `screens.md` are absent from this Companion install. The four blocks below are written by hand in the
shape that script documents.

## Project Structure

```text
MTM_Waitlist.Shared/
  MTM_Waitlist.Shared.csproj         add DevWinUI package reference
  Dialogs/
    MessageKind.cs                   the six kinds
    MessageDialogRequest.cs          what to ask
    MessageDialogOutcome.cs          what came back
    MessageDialogHost.cs             the one entry point every screen calls
    MessageDialogContent.xaml(.cs)   the standard body, shared by all six kinds
    WindowedDialogHost.cs            wraps any ContentDialog in an owned window
Styles/
  Dialogs.xaml                       dialog sizing + message chrome (new)
  Thickness.xaml                     already declares PageContentMaxWidth
  ModuleSetup.xaml                   SetupPageGridStyle, already carries the cap
App.xaml                             merge Styles/Dialogs.xaml
Module_Core/Views/ShellPage.xaml(.cs)              raise sites -> shared set
Module_Startup/Views/*.xaml(.cs)                   raise sites -> shared set
Module_Settings/Views/*.xaml(.cs)                  raise sites -> shared set
Module_Setup/Views/*.xaml(.cs)                     raise sites -> shared set
Module_Waitlist/Views/*.xaml(.cs)                  raise sites -> shared set
Module_Shared/Views/ControlInspectorDetailWindow    proportional columns, shared type
MTM_Waitlist.Mock.Service/                          project ref to Shared, raise site converted
MTM_Waitlist.Tests/                                 new audits + conversion tests
```

**Structure Decision**: the shared message set goes in `MTM_Waitlist.Shared` because every module and the application
already reference it, and the service console can reference it without pulling in the operator application. Dialog
sizing lives in a new `Styles/Dialogs.xaml` rather than in `Thickness.xaml`, because it is a size scale of its own and
its registration in `App.xaml` is the change the api rules require to happen in the same commit.

## Call paths

```text
screen code-behind
  -> MessageDialogHost.AskAsync(owner, request)
       -> MessageDialogContent  (the standard body)
       -> ContentDialog         (the api rules' requirement, satisfied)
       -> WindowedContentDialog (the request's requirement: own window, centred)
  -> MessageDialogOutcome  (Act | Cancel | Acknowledged)
```

Every raise site calls the host. No screen constructs a `ContentDialog` directly after this change.

## Code pins

| Location | Why it needs a note |
|---|---|
| `MessageDialogHost` | Owns the whole contract: one call, six kinds, one answer shape. Everything else is a caller. |
| `WindowedDialogHost` | The only place `Owner = window` is set. If it is ever bypassed, dialogs stop following the main window. |
| `MessageDialogContent` | The only place button order and default action are decided. A caller may supply labels, never order. |
| `Styles/Dialogs.xaml` | The four sizing values. FR-004 fails if a dialog hard-codes its own. |

The sizing keys it declares, pinned by FR-004 and copied from the contract unchanged:

| Key | Value |
|---|---|
| `DialogMinWidth` | 480 |
| `DialogPreferredWidth` | 720 |
| `DialogMaxWidth` | 880 |
| `DialogMaxBodyHeight` | 640 |
| `DialogBodyMinHeight` | 120 |

## States

```text
Closed -> Open: a screen calls the host with a request
Open -> Closed: the person chooses an action, presses Escape, or the owner closes
```

The dialog itself has no other state. The answer is the only thing that crosses the boundary, and it is read from the
`ContentDialogResult` the host maps back to a `MessageDialogOutcome`.

## Screens

```text
message Acknowledge  One action, OK
message Confirm      Act and Cancel, Act default
message Decide       Yes and No, Yes default
message Destructive  Act and Cancel, Cancel default
message Leave        Leave and Stay, Stay default
message Retry        Try again and Cancel, Try again default
```

Each of the six is one configuration of the same body, not six files. Every converted dialog (the picture pickers, the
search dialogs, the lugger editor, the user and computer editors) is a screen of its own already and keeps its layout,
gaining only the windowed host and the shared sizing.

## Constitution Check

| Principle | Assessment |
|---|---|
| I. Spec-First, Verified Delivery | PASS. Spec written and clarified first; tasks and their ticks follow the build and test gates. |
| II. Live Data Integrity | PASS. No store read or write is touched. No mock mode is introduced; the service console's own behaviour is unchanged. |
| III. Stored-Procedure-First | PASS. No database artifact changes. |
| IV. MCP-First, Grounded Decisions | PASS. `WindowedContentDialog` usage is copied from the working implementation in this repo, and the DevWinUI surface is checked against its documentation before the host is written. |
| V. WinUI 3 Platform Conformance | PASS, and this principle is the feature's purpose. `Microsoft.UI.Xaml` only, MVVM preserved, resources registered in `App.xaml` in the same change, fluids and `Min*`/`Max*` bounds, theme-backed text styles, localized strings. |
| VI. Evidence-Based Verification Gates | PASS. Clean x64 build with zero warnings, green unit suite, and three new source audits run in that suite. `WMC9999` is treated as a real XAML error. |
| VII. Readable, End-User-Facing Delivery | PASS. Remaining replies state what changed and what is still open, with outstanding actions last. |

No unresolved gate failures, so no Complexity Tracking table is needed.

## Implementation notes — what the build taught us

Recorded during `/speckit.companion.implement`, because each of these changed a decision the plan had made. Every one is
verified: the build is clean with zero warnings and the suite is green (2412 passed, 56 skipped, 0 failed).

### N1 — DevWinUI cannot be referenced from a class library at WindowsAppSDK 2.3.1

`applying <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` to a class library is refused outright, and
without it the component check rejects DevWinUI 10.8.0, whose own dependency set is `Microsoft.WindowsAppSDK.Foundation
2.3.9` and `Microsoft.WindowsAppSDK.WinUI 2.3.6` against a 2.3.1 runtime that expects 2.3.5 and 2.3.0.

Resolved by moving the umbrella package to **2.4.0 across all twelve projects**, because `Microsoft.WindowsAppSDK.Runtime
2.4.0` expects exactly Foundation 2.3.9 and WinUI 2.3.6 — the set DevWinUI already forces. A partial bump is not
possible: NuGet unifies the version upwards, so any single project left at 2.3.1 fails with NU1605. The runtime the
application ships was already those assemblies; 2.4.0 aligns the manifests with them.

### N2 — XAML in a referenced class library is compiled twice by a root project

`MTM_Waitlist.csproj` sits at the repository root, so its default `**/*.xaml`, `**/*.pri` and content globs reach into
every sibling project — including their `bin\` folders, which `DefaultItemExcludes` covers only for this project's own
output. The shared message body produced WMC0610, then PRI175/PRI277, then CS0436. Fixed by removing
`MTM_Waitlist.Shared\**\*` and every nested `bin\`/`obj\` from the `Page`, `ApplicationDefinition`, `PRIResource`,
`Content` and `None` globs. The library ships its own `.pri`, which is how its XAML reaches the application.

### N3 — `WindowedContentDialog` is not a `ContentDialog`, and that changes three dialogs

Its public surface was read from the package, not assumed: `Header`, `Content`, three `*ButtonContent` slots, three
`*ButtonCommand` slots, `IsPrimaryButtonEnabled`, `DefaultButton`, `Owner`, `Result`, `HasTitleBar`, `CanResize`,
`MinWidth`, `MinHeight`, `ShowAsync() → Task<ContentDialogResult>`, and `PrimaryButtonClick`/`SecondaryButtonClick`/
`CloseButtonClick` as plain `EventHandler`. There is **no** `Opened`, `Closed`, `Closing`, or `Hide()`, and the button
event fires **after** the window has already closed, so a press cannot be refused.

Three behaviours therefore had to change, and each is reported rather than hidden:

- **A failed save can no longer hold its window open.** The two picture editors and the lugger name question now run
  their write after the window closes and, if it is refused, say so through the `Retry` message and reopen the editor
  over the edits the view model still holds. Nothing typed is lost; the person sees the same sentence they used to see
  inside the window, one window later.
- **The dunnage image search gained an accept button** (`Use this part`, disabled until a card is chosen), because a
  card click used to close the window and the window can no longer be closed from its content.
- **A double-click in the work-order search selects rather than closes**, for the same reason.

### N4 — the service console does not merge `Styles/Dialogs.xaml`

It is a separate application with its own PRI, so a `Source` pointing into the operator application's project resolves
to nothing. `DialogSizing` reads the five keys when the dictionary is present and falls back to the same five numbers
when it is not, which keeps the console's dialogs the same size without a second copy of the file.

### N5 — the shell is the frame, not a content screen

`Module_Core/Views/ShellPage.xaml` holds the navigation pane and the page frame; capping it would cap the pane with it.
Every page it navigates to carries the ceiling, and the extended page-content audit requires that of every page the
inventory lists.

### N6 — the kind decides the slot, the screen decides the word

FR-009 fixes the order, the position and the default per kind; FR-027 preserves each screen's wording. Both hold because
a screen may name its own affirmative and dismissive labels — "Remove", "Clear all", "Yes" — while the kind still
decides which slot they sit in and which one Enter takes.

### N7 — the shell and the page columns stretch, and now say so

Reported after the run: the page in the shell read as if it had a minimum width the window could not widen. Two silent
causes, both of which leave every other check green.

A page-level `ScrollViewer` measures its child with **unlimited width** unless horizontal scrolling is switched off, so
the capped column sized to its own content rather than to the viewport. `CreateUserPage`, `EditUserPage` and the two
service console pages had exactly that, because `HorizontalScrollMode` defaults to `Enabled`. And a capped element that
also carries `HorizontalAlignment="Left"` is content-sized too, because alignment overrides the stretch that would
otherwise fill the column — the same trap the design system already records for `MaxWidth` with `Center`.

Fixed by switching horizontal scrolling off on those four pages, dropping the left alignment, and stating the stretch on
the shell's chain — `ShellPage`'s root grid, the `NavigationView` with its content alignment, and the `NavigationFrame`
with its content alignment — so the frame is tied to the window whatever is placed inside it. Three audits in
`PageContentWidthAuditTests` keep it that way: the shell host must stretch in both directions, no page may measure its
column unconstrained, and no page may pin its capped column.

### N8 — the page ceiling is removed: the shell's contents expand, full stop (supersedes N5 and the cap half of N7)

The owner asked for this plainly after seeing the running app: **the shell's contents expand horizontally without
question**. The 1080-px ceiling that had been added earlier — because on a 2900-px window a card spanned the whole width —
is therefore gone: the resource, the setter, and every page's reference to it. A review of the running app confirms the
page now fills: at a 2400-px window the waitlist list measures 2011 px and its card 1999 px, and at 3456 px the card is
3055 px.

Removing the ceiling exposed the fault the ceiling had been masking, so it was fixed at its cause rather than left
behind. The navigation pane switched display mode at `ExpandedModeThresholdWidth`, and when it did it took 260 px out of
the page: measured on 2026-10-25, the page went from **1311 px at a 1519-px window to 1119 px at 1520**. With a capped
column that was invisible, because the page had slack. It is not invisible now. The pane therefore has one display mode —
`PaneDisplayMode="Left"`, always expanded, one width — and the window's own `MinWidth` was raised to 900 so the pane can
never squeeze the page out of existence. Swept on the running app after the change: 1300 → 911, 1499 → 1110,
1500 → 1111, 1520 → 1131, 1700 → 1311, 2400 → 2011. Monotonic, with no inversion at any crossing.

`PageContentWidthAuditTests` was rewritten to the new policy rather than deleted: the ceiling must be gone everywhere, a
setup step must stretch and carry no maximum, no page may pin or cap its content, no page may measure its content
unconstrained, the shell host must stretch in both directions, and the pane must never switch mode.
`ShellHostingAuditTests.WideningTheWindowNeverNarrowsThePage` now asserts the pane has one mode and one width, and that
the window's minimum leaves the page at least 300 px.
