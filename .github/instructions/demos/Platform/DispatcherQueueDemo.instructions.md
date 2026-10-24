---
description: "DispatcherQueue (Microsoft.UI.Dispatching.DispatcherQueue) in Platform services; the UI thread's work queue. Every XAML touch from a background… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/DispatcherQueueDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/DispatcherQueueDemo.xaml.cs"
---

# DispatcherQueue

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Intermediate |
| API | `Microsoft.UI.Dispatching.DispatcherQueue` |
| Page | `Platform/DispatcherQueueDemo` (`WinUI3LearningLab.Demos.Platform.DispatcherQueueDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo dispatcher-queue` |

## What it teaches

The UI thread's work queue. Every XAML touch from a background thread has to be marshalled through it.

## How the demo is built

The page is a `UserControl` at `Platform/DispatcherQueueDemo.xaml`, paired with `DispatcherQueueDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `RunButton`, `Progress`, `ThreadText`, `PriorityText`, `LastActionText`.

Wired in XAML: `OnRunClicked()`, `OnCompareClicked()`.

**Code-behind.** Handlers `OnRunClicked()`, `OnCompareClicked()`. 130 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- GetForCurrentThread returns null on a thread that has no queue — capture the UI queue once at start-up.
- TryEnqueue fails (returns false) when the queue is shutting down, so check the result instead of assuming.
- DispatcherQueue has no SynchronizationContext by itself in early WinUI 3 versions; awaiting a Task on a background thread does not return to the UI thread.
- EnqueuePriority.Low is right for non-urgent refreshes and keeps input responsive.

## Try this

- Run `WinUI3LearningLab.App.exe --demo dispatcher-queue`. The page shows `DispatcherQueueDemo.xaml` and `DispatcherQueueDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Dispatching.DispatcherQueue](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueue)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
