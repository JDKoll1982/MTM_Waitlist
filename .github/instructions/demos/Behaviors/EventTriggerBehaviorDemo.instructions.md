---
description: "EventTriggerBehavior (Microsoft.Xaml.Interactivity.EventTriggerBehavior) in Triggers & actions; raises an action when an event fires, so an event can be… MTM: MTM_Receiving_Application › Receiving › entry validation."
applyTo: "src/WinUI3LearningLab.App/Demos/Behaviors/EventTriggerBehaviorDemo.xaml,src/WinUI3LearningLab.App/Demos/Behaviors/EventTriggerBehaviorDemo.xaml.cs"
---

# EventTriggerBehavior

| | |
| --- | --- |
| Ecosystem | XAML Behaviors |
| Category | Triggers & actions |
| Level | Intermediate |
| API | `Microsoft.Xaml.Interactivity.EventTriggerBehavior` |
| Page | `Behaviors/EventTriggerBehaviorDemo` (`WinUI3LearningLab.Demos.Behaviors.EventTriggerBehaviorDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo event-trigger` |

## What it teaches

Raises an action when an event fires, so an event can be wired to a command from XAML with no code-behind.

## How the demo is built

The page is a `UserControl` at `Behaviors/EventTriggerBehaviorDemo.xaml`, paired with `EventTriggerBehaviorDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

**Code-behind.** 39 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › entry validation**

Quantity bounds, required fields and focus order are the same on eight entry screens. Expressing them declaratively keeps the eight screens consistent and keeps the code-behind to event handlers that actually do something.

Focus moves to the quantity box the moment a scan completes, without a line of code-behind on the entry form.

Schema this mirrors: `dunnage_specs, receiving_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The event name is a string in XAML — a typo compiles and then silently never fires.
- Attach with the attached property `Interaction.Behaviors`; adding a Behavior object as a child of the control does nothing.
- Pair it with InvokeCommandAction to reach a view-model command; that is the classic no-code-behind wiring.
- Behaviors are invisible in the visual tree, which makes an unexplained reaction harder to trace than a plain event handler.

## Try this

- Change the EventName to a typo and confirm nothing happens — that is the failure mode to watch for.

## Documentation

- [Microsoft.Xaml.Interactivity.EventTriggerBehavior](https://github.com/microsoft/XamlBehaviors)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Behaviors.cs` or the demo source, then regenerate.
