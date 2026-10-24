---
description: "ObservableValidator (CommunityToolkit.Mvvm.ComponentModel.ObservableValidator) in ObservableObject & commands; data-annotation validation wired into INotifyDataErrorInfo… MTM: MTM_WIP_Application_WinForms › Inventory › transaction entry."
applyTo: "src/WinUI3LearningLab.App/Demos/Mvvm/ObservableValidatorDemo.xaml,src/WinUI3LearningLab.App/Demos/Mvvm/ObservableValidatorDemo.xaml.cs"
---

# ObservableValidator

| | |
| --- | --- |
| Ecosystem | Community Toolkit.Mvvm |
| Category | ObservableObject & commands |
| Level | Advanced |
| API | `CommunityToolkit.Mvvm.ComponentModel.ObservableValidator` |
| Page | `Mvvm/ObservableValidatorDemo` (`WinUI3LearningLab.Demos.Mvvm.ObservableValidatorDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo observable-validator` |

## What it teaches

Data-annotation validation wired into INotifyDataErrorInfo, so error text reaches the control.

## How the demo is built

The page is a `UserControl` at `Mvvm/ObservableValidatorDemo.xaml`, paired with `ObservableValidatorDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 20 element(s).

**Code-behind.** Handlers `OnErrorsChanged()`. 82 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › transaction entry**

A transaction is only valid when part, bin, quantity and operation are all present, so the commit command is disabled until the view model says otherwise. State changes have to reach the grid without the code-behind touching it.

Schema this mirrors: `inv_transaction, inv_inventory, inv_inventory_batch_seq`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Validation runs on SetProperty only when you call the generated setter with `validate: true`.
- The XAML side needs the binding to surface errors; a missing Binding.ValidatesOnNotifyDataErrors leaves the message invisible.
- Validating on every keystroke marks a field invalid before the user finishes typing — validate on lost focus for text input.

## Try this

- Run `WinUI3LearningLab.App.exe --demo observable-validator`. The page shows `ObservableValidatorDemo.xaml` and `ObservableValidatorDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.Mvvm.ComponentModel.ObservableValidator](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Mvvm.cs` or the demo source, then regenerate.
