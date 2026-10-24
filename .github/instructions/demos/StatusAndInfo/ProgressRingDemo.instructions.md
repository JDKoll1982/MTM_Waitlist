---
description: "ProgressRing (Microsoft.UI.Xaml.Controls.ProgressRing) in Status & info; circular progress indicator, used for short waits and inside buttons or… MTM: MTM_Waitlist › Waitlist › request state and audit."
applyTo: "src/WinUI3LearningLab.App/Demos/StatusAndInfo/ProgressRingDemo.xaml,src/WinUI3LearningLab.App/Demos/StatusAndInfo/ProgressRingDemo.xaml.cs"
---

# ProgressRing

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Status & info |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ProgressRing` |
| Page | `StatusAndInfo/ProgressRingDemo` (`WinUI3LearningLab.Demos.StatusAndInfo.ProgressRingDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo progress-ring` |

## What it teaches

Circular progress indicator, used for short waits and inside buttons or tiles.

## How the demo is built

The page is a `UserControl` at `StatusAndInfo/ProgressRingDemo.xaml`, paired with `ProgressRingDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 24 element(s).

Named elements the code-behind touches: `MainRing`, `ActiveToggle`, `DeterminateToggle`, `RingSlider`, `RingStateText`.

Wired in XAML: `OnRingStateChanged()`, `OnRingValueChanged()`.

**Code-behind.** Handlers `OnRingStateChanged()`, `OnRingValueChanged()`. 48 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › request state and audit**

A request moves through states and every transition is audited. The screen has to say what state a request is in, whether something is still running, and what failed — without a modal box per event.

Schema this mirrors: `waitlist_requests_audit, waitlist_defect_types, log_error`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- IsActive is the on/off switch; a ring that is present but inactive occupies space and renders nothing.
- Determinate mode (IsIndeterminate=False) needs Value and Maximum; the default maximum of 100 catches people out.
- Prefer ProgressBar for long tasks with a known percentage and ProgressRing for short, indeterminate waits.

## Try this

- Run `WinUI3LearningLab.App.exe --demo progress-ring`. The page shows `ProgressRingDemo.xaml` and `ProgressRingDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ProgressRing](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.progressring)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/StatusAndInfo.cs` or the demo source, then regenerate.
