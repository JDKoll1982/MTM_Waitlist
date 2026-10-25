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
