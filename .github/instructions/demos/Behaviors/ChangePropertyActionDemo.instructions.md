---
description: "ChangePropertyAction (Microsoft.Xaml.Interactions.Core.ChangePropertyAction) in Triggers & actions; sets a property on a target element when triggered —… MTM: MTM_Receiving_Application › Receiving › entry validation."
applyTo: "src/WinUI3LearningLab.App/Demos/Behaviors/ChangePropertyActionDemo.xaml,src/WinUI3LearningLab.App/Demos/Behaviors/ChangePropertyActionDemo.xaml.cs"
---

# ChangePropertyAction

| | |
| --- | --- |
| Ecosystem | XAML Behaviors |
| Category | Triggers & actions |
| Level | Starter |
| API | `Microsoft.Xaml.Interactions.Core.ChangePropertyAction` |
| Page | `Behaviors/ChangePropertyActionDemo` (`WinUI3LearningLab.Demos.Behaviors.ChangePropertyActionDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo change-property-action` |

## What it teaches

Sets a property on a target element when triggered — declarative state changes for small interactions.

## How the demo is built

The page is a `UserControl` at `Behaviors/ChangePropertyActionDemo.xaml`, paired with `ChangePropertyActionDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 48 element(s).

Named elements the code-behind touches: `StateText`, `StateHost`, `StateFrame`, `ReceiptStates`, `ReceivedState`, `HeldState`, `StateGlyph`, `StateLabel`, `StateDetail`, `StateTraceText`.

Wired in XAML: `OnReceivedState()`, `OnHeldState()`.

**Code-behind.** Handlers `OnReceivedState()`, `OnHeldState()`. 36 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › entry validation**

Quantity bounds, required fields and focus order are the same on eight entry screens. Expressing them declaratively keeps the eight screens consistent and keeps the code-behind to event handlers that actually do something.

Schema this mirrors: `dunnage_specs, receiving_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It sets the property once per trigger; it does not restore the previous value, so a toggle needs two triggers.
- Property values are strings in XAML and are converted — an invalid value fails at runtime, not at compile time.
- For anything with more than two states a VisualStateManager is clearer than a growing trigger list.

## Try this

- Run `WinUI3LearningLab.App.exe --demo change-property-action`. The page shows `ChangePropertyActionDemo.xaml` and `ChangePropertyActionDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.Xaml.Interactions.Core.ChangePropertyAction](https://github.com/microsoft/XamlBehaviors)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Behaviors.cs` or the demo source, then regenerate.
