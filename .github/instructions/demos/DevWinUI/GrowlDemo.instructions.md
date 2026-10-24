---
description: "Growl (DevWinUI.Growl) in Controls; an in-window notification stack, for messages that must not interrupt but must not be missed. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/GrowlDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/GrowlDemo.xaml.cs"
---

# Growl

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.Growl` |
| Page | `DevWinUI/GrowlDemo` (`WinUI3LearningLab.Demos.DevWinUI.GrowlDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo growl` |

## What it teaches

An in-window notification stack, for messages that must not interrupt but must not be missed.

## How the demo is built

The page is a `UserControl` at `DevWinUI/GrowlDemo.xaml`, paired with `GrowlDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 11 element(s).

Named elements the code-behind touches: `GrowlHost`, `StackText`, `AnnounceText`.

Wired in XAML: `OnInfoClick()`, `OnSuccessClick()`, `OnWarningClick()`, `OnClearClick()`.

**Code-behind.** Handlers `OnInfoClick()`, `OnSuccessClick()`, `OnWarningClick()`, `OnClearClick()`, `OnUnloaded()`. 92 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Growl shows inside your window, so it is invisible when the app is in the background — that is what app notifications are for.
- Messages stack; an unbounded stack becomes noise. Cap it and dismiss the oldest first.
- Announce the message to assistive technology as well; a growl is not in the tab order.

## Traps recorded in the source

- New growls are inserted at index 0, so the oldest one is always the last child.

## Try this

- Run `WinUI3LearningLab.App.exe --demo growl`. The page shows `GrowlDemo.xaml` and `GrowlDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Growl](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
