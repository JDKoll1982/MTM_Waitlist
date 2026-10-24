---
description: "ProgressButton (DevWinUI.ProgressButton) in Controls; a button that owns its own busy state, so a submit action can show progress without a separate… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/ProgressButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/ProgressButtonDemo.xaml.cs"
---

# ProgressButton

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.ProgressButton` |
| Page | `DevWinUI/ProgressButtonDemo` (`WinUI3LearningLab.Demos.DevWinUI.ProgressButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo progress-button` |

## What it teaches

A button that owns its own busy state, so a submit action can show progress without a separate ProgressRing.

## How the demo is built

The page is a `UserControl` at `DevWinUI/ProgressButtonDemo.xaml`, paired with `ProgressButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 7 element(s).

Named elements the code-behind touches: `PostButton`, `FailCheck`, `StateText`.

Wired in XAML: `OnPostClicked()`.

**Code-behind.** Handlers `OnPostClicked()`. 59 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- There is no IsBusy member: the busy state IS IsChecked, and Progress is a separate determinate value. Driving the wrong one leaves the button looking idle while work runs.
- It does not refuse a second click by itself — disable it (or guard in the command) or a double click submits twice.
- Settle the busy state in a finally block; a failed submit that leaves the button spinning is worse than the failure.

## Try this

- Run `WinUI3LearningLab.App.exe --demo progress-button`. The page shows `ProgressButtonDemo.xaml` and `ProgressButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.ProgressButton](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
