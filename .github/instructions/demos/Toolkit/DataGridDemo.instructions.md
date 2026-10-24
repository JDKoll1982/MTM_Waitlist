---
description: "DataGrid (CommunityToolkit.WinUI.UI.Controls.DataGrid) in Controls; a real tabular grid with columns, sorting, resizing and inline… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/DataGridDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/DataGridDemo.xaml.cs"
---

# DataGrid

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Advanced |
| API | `CommunityToolkit.WinUI.UI.Controls.DataGrid` |
| Page | `Toolkit/DataGridDemo` (`WinUI3LearningLab.Demos.Toolkit.DataGridDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo data-grid` |

## What it teaches

A real tabular grid with columns, sorting, resizing and inline editing — the thing WinUI 3 itself does not ship.

## How the demo is built

The page is a `UserControl` at `Toolkit/DataGridDemo.xaml`, paired with `DataGridDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `Grid`, `SelectionText`, `LastActionText`.

Wired in XAML: `OnSelectionChanged()`, `OnAddClicked()`, `OnRemoveClicked()`, `OnSortClicked()`.

**Code-behind.** Handlers `OnSelectionChanged()`, `OnAddClicked()`, `OnRemoveClicked()`, `OnSortClicked()`. 83 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

The stock list: every column the user has reordered and resized is persisted to `usr_dgv_settings`, and the grid binds `inv_inventory` rows without a view model per cell.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- AutoGenerateColumns only inspects public properties; for computed columns declare DataGridTextColumn with Binding.
- Binding inside DataGrid columns uses classic {Binding}, not {x:Bind}, because the columns are not in the item template's scope.
- IsReadOnly defaults to false: users can edit cells unless you say otherwise.
- Row height and column widths are not virtualised the way a ListView is — very large tables get slow.

## Try this

- Run `WinUI3LearningLab.App.exe --demo data-grid`. The page shows `DataGridDemo.xaml` and `DataGridDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.DataGrid](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
