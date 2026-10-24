---
description: "StepBar (DevWinUI.StepBar) in Controls; an ordered, read-only progress trail for a multi-step flow — the shape a receiving header or a waitlist intake already… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/StepBarDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/StepBarDemo.xaml.cs"
---

# StepBar

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.StepBar` |
| Page | `DevWinUI/StepBarDemo` (`WinUI3LearningLab.Demos.DevWinUI.StepBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo step-bar` |

## What it teaches

An ordered, read-only progress trail for a multi-step flow — the shape a receiving header or a waitlist intake already has.

## How the demo is built

The page is a `UserControl` at `DevWinUI/StepBarDemo.xaml`, paired with `StepBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `BackButton`, `NextButton`, `PositionText`, `FlowTrail`, `StepTitle`, `StepDetail`, `ResourceWarning`.

Wired in XAML: `OnBackClick()`, `OnNextClick()`.

**Code-behind.** Handlers `OnBackClick()`, `OnNextClick()`. 120 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

The receiving wizard is four steps — scan, dunnage, quantity, post — and the step bar is how the operator knows where they are when a scan fails at step two.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- StepBar is display-only: it has no SelectedIndex you set to navigate. You drive it by rebuilding its Steps (StepBarItem) as the flow advances.
- StepBarItem carries Icon and text; the current step is styled by the control's own states, so do not recolour individual items.
- It does not scroll. A flow with more than about six steps has to be shortened or wrapped in a horizontal ScrollViewer.

## Try this

- Add a Back button and rebuild Steps so the trail rewinds — the control follows the data, not the click.

## Documentation

- [DevWinUI.StepBar](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
