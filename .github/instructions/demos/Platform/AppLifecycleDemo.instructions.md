---
description: "App lifecycle & single instance (Microsoft.Windows.AppLifecycle.AppInstance) in Platform services; how a Windows App SDK app starts, is activated and can… MTM: MTM_Waitlist › Settings persistence and start-up."
applyTo: "src/WinUI3LearningLab.App/Demos/Platform/AppLifecycleDemo.xaml,src/WinUI3LearningLab.App/Demos/Platform/AppLifecycleDemo.xaml.cs"
---

# App lifecycle & single instance

| | |
| --- | --- |
| Ecosystem | Windows App SDK |
| Category | Platform services |
| Level | Advanced |
| API | `Microsoft.Windows.AppLifecycle.AppInstance` |
| Page | `Platform/AppLifecycleDemo` (`WinUI3LearningLab.Demos.Platform.AppLifecycleDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo app-lifecycle` |

## What it teaches

How a Windows App SDK app starts, is activated and can redirect a second launch into the first instance.

## How the demo is built

The page is a `UserControl` at `Platform/AppLifecycleDemo.xaml`, paired with `AppLifecycleDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `FactsText`, `ActivationText`, `LastActionText`.

Wired in XAML: `OnProbeActivationClicked()`, `OnListInstancesClicked()`.

**Code-behind.** Handlers `OnProbeActivationClicked()`, `OnListInstancesClicked()`. 86 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Settings persistence and start-up**

Effective settings are resolved from several scopes and cached at start-up, and every change is versioned in a history table. Persistence, the UI thread and theme resolution are the plumbing that has to keep working when nothing is being demoed.

Schema this mirrors: `config_settings_values, config_settings_history, usr_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Call AppInstance.FindOrRegisterForKey and RedirectActivationToAsync early — before any UI exists.
- Unpackaged apps receive command-line activations; packaged ones receive activation kinds such as Protocol or File.
- The redirect must happen on the main instance's thread; blocking it with .Result deadlocks.
- This lab itself is single-instance unsupported on purpose — the demo shows the pattern, it does not change the app's own behaviour.

## Try this

- Run `WinUI3LearningLab.App.exe --demo app-lifecycle`. The page shows `AppLifecycleDemo.xaml` and `AppLifecycleDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Windows.AppLifecycle.AppInstance](https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Platform.cs` or the demo source, then regenerate.
