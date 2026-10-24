---
description: "RelayCommand (CommunityToolkit.Mvvm.Input.RelayCommand) in ObservableObject & commands; source-generated ICommand implementations, including async… MTM: MTM_WIP_Application_WinForms › Inventory › transaction entry."
applyTo: "src/WinUI3LearningLab.App/Demos/Mvvm/RelayCommandDemo.xaml,src/WinUI3LearningLab.App/Demos/Mvvm/RelayCommandDemo.xaml.cs"
---

# RelayCommand

| | |
| --- | --- |
| Ecosystem | Community Toolkit.Mvvm |
| Category | ObservableObject & commands |
| Level | Starter |
| API | `CommunityToolkit.Mvvm.Input.RelayCommand` |
| Page | `Mvvm/RelayCommandDemo` (`WinUI3LearningLab.Demos.Mvvm.RelayCommandDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo relay-command` |

## What it teaches

Source-generated ICommand implementations, including async commands with re-entrancy protection.

## How the demo is built

The page is a `UserControl` at `Mvvm/RelayCommandDemo.xaml`, paired with `RelayCommandDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 20 element(s).

**Code-behind.** 57 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › transaction entry**

A transaction is only valid when part, bin, quantity and operation are all present, so the commit command is disabled until the view model says otherwise. State changes have to reach the grid without the code-behind touching it.

**Post transaction** is a `RelayCommand` whose `CanExecute` requires part, bin, quantity and operation — the same four columns that are `NOT NULL` in `inv_transaction`.

Schema this mirrors: `inv_transaction, inv_inventory, inv_inventory_batch_seq`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The generated member name appends 'Command' and strips 'Async': a method `Save` becomes `SaveCommand`, and `SaveAsync` becomes `SaveCommand` too — not `SaveAsyncCommand`.
- On an async method the generator produces AsyncRelayCommand, whose CanExecute is false while running — that is the re-entrancy guard.
- CanExecute is re-queried automatically when a property on the same class changes, which is why it pairs with [ObservableProperty].
- Do not also disable the control by hand: two guards drift apart as soon as one is edited.

## Try this

- Run `WinUI3LearningLab.App.exe --demo relay-command`. The page shows `RelayCommandDemo.xaml` and `RelayCommandDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.Mvvm.Input.RelayCommand](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Mvvm.cs` or the demo source, then regenerate.
