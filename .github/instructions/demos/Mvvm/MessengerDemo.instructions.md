---
description: "WeakReferenceMessenger (CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger) in ObservableObject & commands; decoupled publish/subscribe between view models, with weak… MTM: MTM_WIP_Application_WinForms › Inventory › transaction entry."
applyTo: "src/WinUI3LearningLab.App/Demos/Mvvm/MessengerDemo.xaml,src/WinUI3LearningLab.App/Demos/Mvvm/MessengerDemo.xaml.cs"
---

# WeakReferenceMessenger

| | |
| --- | --- |
| Ecosystem | Community Toolkit.Mvvm |
| Category | ObservableObject & commands |
| Level | Intermediate |
| API | `CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger` |
| Page | `Mvvm/MessengerDemo` (`WinUI3LearningLab.Demos.Mvvm.MessengerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo messenger` |

## What it teaches

Decoupled publish/subscribe between view models, with weak references so subscribers can still be collected.

## How the demo is built

The page is a `UserControl` at `Mvvm/MessengerDemo.xaml`, paired with `MessengerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 24 element(s).

**Code-behind.** 106 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › transaction entry**

A transaction is only valid when part, bin, quantity and operation are all present, so the commit command is disabled until the view model says otherwise. State changes have to reach the grid without the code-behind touching it.

Schema this mirrors: `inv_transaction, inv_inventory, inv_inventory_batch_seq`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Default sends to every registered handler; use a value-type message or a dedicated message type so listeners are unambiguous.
- Weak references mean a handler can vanish when its view model is collected — which is the point, but it surprises people debugging a missing message.
- Never send a message from a constructor: nothing has subscribed yet.
- A message that is only ever handled in one place is better as a plain method call.

## Try this

- Run `WinUI3LearningLab.App.exe --demo messenger`. The page shows `MessengerDemo.xaml` and `MessengerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Mvvm.cs` or the demo source, then regenerate.
