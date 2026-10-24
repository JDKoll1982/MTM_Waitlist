---
description: "ObservableProperty (CommunityToolkit.Mvvm.ComponentModel.ObservableObject) in ObservableObject & commands; source-generated properties that raise PropertyChanged… MTM: MTM_WIP_Application_WinForms › Inventory › transaction entry."
applyTo: "src/WinUI3LearningLab.App/Demos/Mvvm/ObservablePropertyDemo.xaml,src/WinUI3LearningLab.App/Demos/Mvvm/ObservablePropertyDemo.xaml.cs"
---

# ObservableProperty

| | |
| --- | --- |
| Ecosystem | Community Toolkit.Mvvm |
| Category | ObservableObject & commands |
| Level | Starter |
| API | `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` |
| Page | `Mvvm/ObservablePropertyDemo` (`WinUI3LearningLab.Demos.Mvvm.ObservablePropertyDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo observable-property` |

## What it teaches

Source-generated properties that raise PropertyChanged, replacing the hand-written backing field and setter.

## How the demo is built

The page is a `UserControl` at `Mvvm/ObservablePropertyDemo.xaml`, paired with `ObservablePropertyDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 27 element(s).

**Code-behind.** Handlers `OnViewModelPropertyChanged()`. 74 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › transaction entry**

A transaction is only valid when part, bin, quantity and operation are all present, so the commit command is disabled until the view model says otherwise. State changes have to reach the grid without the code-behind touching it.

A transaction row in `inv_inventory` is edited in place; the view model raises `PropertyChanged` so the grid updates without a refresh query.

Schema this mirrors: `inv_transaction, inv_inventory, inv_inventory_batch_seq`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The containing class must be partial — a missing partial is a build error inside generated code, which reads confusingly.
- The generated property name is the field's name without the underscore, capitalised: `_partNumber` becomes `PartNumber`.
- Use `[NotifyPropertyChangedFor(nameof(IsValid))]` for dependent values instead of raising the second change by hand.
- The setter compares with EqualityComparer, so assigning an equal value raises nothing — do not rely on the change notification for repeating actions.

## Traps recorded in the source

- SetProperty compares with EqualityComparer, so this assignment raises nothing at all.

## Try this

- Add a second generated property and a [NotifyPropertyChangedFor] link between them.

## Documentation

- [CommunityToolkit.Mvvm.ComponentModel.ObservableObject](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Mvvm.cs` or the demo source, then regenerate.
