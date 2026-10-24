---
description: "ProgressBar (Microsoft.UI.Xaml.Controls.ProgressBar) in Status & info; horizontal progress, determinate or indeterminate, with an optional… MTM: MTM_Waitlist › Waitlist › request state and audit."
applyTo: "src/WinUI3LearningLab.App/Demos/StatusAndInfo/ProgressBarDemo.xaml,src/WinUI3LearningLab.App/Demos/StatusAndInfo/ProgressBarDemo.xaml.cs"
---

# ProgressBar

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Status & info |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ProgressBar` |
| Page | `StatusAndInfo/ProgressBarDemo` (`WinUI3LearningLab.Demos.StatusAndInfo.ProgressBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo progress-bar` |

## What it teaches

Horizontal progress, determinate or indeterminate, with an optional paused/error visual state.

## How the demo is built

The page is a `UserControl` at `StatusAndInfo/ProgressBarDemo.xaml`, paired with `ProgressBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `MainBar`, `ValueSlider`, `RunText`, `IndeterminateToggle`, `PausedToggle`, `ErrorToggle`, `StateText`.

Wired in XAML: `OnValueChanged()`, `OnStartClicked()`, `OnStopClicked()`, `OnResetClicked()`, `OnVisualStateToggled()`.

**Code-behind.** Handlers `OnUnloaded()`, `OnTick()`, `OnValueChanged()`, `OnStartClicked()`, `OnStopClicked()`, `OnResetClicked()`, `OnVisualStateToggled()`. 103 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › request state and audit**

A request moves through states and every transition is audited. The screen has to say what state a request is in, whether something is still running, and what failed — without a modal box per event.

The nightly export of `receiving_history` reports determinate progress because the row count is known up front; the indeterminate ring is reserved for work whose length is unknown.

Schema this mirrors: `waitlist_requests_audit, waitlist_defect_types, log_error`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- IsIndeterminate ignores Value entirely — the repeating animation is the whole point.
- ShowPaused and ShowError freeze the bar in a distinct visual state; they do not stop your work.
- Use ShowError for a failed state instead of colouring the bar yourself.

## Try this

- Run `WinUI3LearningLab.App.exe --demo progress-bar`. The page shows `ProgressBarDemo.xaml` and `ProgressBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ProgressBar](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.progressbar)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/StatusAndInfo.cs` or the demo source, then regenerate.
