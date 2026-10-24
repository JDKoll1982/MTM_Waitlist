---
description: "InvokeCommandAction (Microsoft.Xaml.Interactions.Core.InvokeCommandAction) in Triggers & actions; calls an ICommand, optionally passing an event argument or a… MTM: MTM_Receiving_Application › Receiving › entry validation."
applyTo: "src/WinUI3LearningLab.App/Demos/Behaviors/InvokeCommandActionDemo.xaml,src/WinUI3LearningLab.App/Demos/Behaviors/InvokeCommandActionDemo.xaml.cs"
---

# InvokeCommandAction

| | |
| --- | --- |
| Ecosystem | XAML Behaviors |
| Category | Triggers & actions |
| Level | Intermediate |
| API | `Microsoft.Xaml.Interactions.Core.InvokeCommandAction` |
| Page | `Behaviors/InvokeCommandActionDemo` (`WinUI3LearningLab.Demos.Behaviors.InvokeCommandActionDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo invoke-command-action` |

## What it teaches

Calls an ICommand, optionally passing an event argument or a bound value as the parameter.

## How the demo is built

The page is a `UserControl` at `Behaviors/InvokeCommandActionDemo.xaml`, paired with `InvokeCommandActionDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 34 element(s).

Named elements the code-behind touches: `LineList`.

Wired in XAML: `OnClearSelection()`.

**Code-behind.** Handlers `OnClearSelection()`. 46 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › entry validation**

Quantity bounds, required fields and focus order are the same on eight entry screens. Expressing them declaratively keeps the eight screens consistent and keeps the code-behind to event handlers that actually do something.

Schema this mirrors: `dunnage_specs, receiving_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- With no CommandParameter, the action forwards the event's arguments rather than null — which is convenient, and a common source of unexpected payloads.
- It does check CanExecute, so an unavailable command is skipped; the button that fired the trigger still looks enabled, though.
- It runs on the UI thread, so a slow command blocks the interaction that triggered it.

## Try this

- Run `WinUI3LearningLab.App.exe --demo invoke-command-action`. The page shows `InvokeCommandActionDemo.xaml` and `InvokeCommandActionDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Xaml.Interactions.Core.InvokeCommandAction](https://github.com/microsoft/XamlBehaviors)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Behaviors.cs` or the demo source, then regenerate.
