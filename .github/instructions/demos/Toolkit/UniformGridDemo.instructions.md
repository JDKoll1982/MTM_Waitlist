---
description: "UniformGrid (CommunityToolkit.WinUI.UI.Controls.UniformGrid) in Controls; lays children out in equal-sized cells, wrapping rows… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/UniformGridDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/UniformGridDemo.xaml.cs"
---

# UniformGrid

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Starter |
| API | `CommunityToolkit.WinUI.UI.Controls.UniformGrid` |
| Page | `Toolkit/UniformGridDemo` (`WinUI3LearningLab.Demos.Toolkit.UniformGridDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo uniform-grid` |

## What it teaches

Lays children out in equal-sized cells, wrapping rows automatically. A Grid without the boilerplate.

## How the demo is built

The page is a `UserControl` at `Toolkit/UniformGridDemo.xaml`, paired with `UniformGridDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 37 element(s).

Named elements the code-behind touches: `ShapeBox`, `Tiles`, `ShapeText`, `LastActionText`.

Wired in XAML: `OnShapeChanged()`.

**Code-behind.** Handlers `OnShapeChanged()`. 57 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Set Rows or Columns (not both) to constrain the shape; leaving both unset picks a square-ish layout.
- FirstColumn shifts the start of the first row, which is how you offset a partial row.
- Cells are equal, so a child larger than its cell is clipped rather than resizing the grid.

## Try this

- Run `WinUI3LearningLab.App.exe --demo uniform-grid`. The page shows `UniformGridDemo.xaml` and `UniformGridDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.UniformGrid](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
