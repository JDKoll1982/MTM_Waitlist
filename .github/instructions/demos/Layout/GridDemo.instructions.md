---
description: "Grid (Microsoft.UI.Xaml.Controls.Grid) in Layout; row/column layout with star sizing, spanning and attached… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/GridDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/GridDemo.xaml.cs"
---

# Grid

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Grid` |
| Page | `Layout/GridDemo` (`WinUI3LearningLab.Demos.Layout.GridDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo grid` |

## What it teaches

Row/column layout with star sizing, spanning and attached positioning. The default choice for real layouts.

## How the demo is built

The page is a `UserControl` at `Layout/GridDemo.xaml`, paired with `GridDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 31 element(s).

Named elements the code-behind touches: `WidthModeBox`, `RowSpacingToggle`, `DemoGrid`, `Column0`, `Column1`, `Column2`, `WidthText`.

Wired in XAML: `OnWidthModeChanged()`, `OnRowSpacingToggled()`, `OnGridSizeChanged()`.

**Code-behind.** Handlers `OnWidthModeChanged()`, `OnRowSpacingToggled()`, `OnGridSizeChanged()`. 56 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Row and column definitions must be declared or attached properties silently do nothing.
- * sizing distributes leftover space; Auto sizes to content and can measure to infinity inside a ScrollViewer.
- Grid.RowSpan/ColumnSpan are attached properties on the child, and an item defaults to row 0 column 0.
- Use RowSpacing/ColumnSpacing instead of margins on children — margins do not collapse and misalign edges.

## Try this

- Change a column from Auto to * and watch the remaining space move.

## Documentation

- [Microsoft.UI.Xaml.Controls.Grid](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.grid)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
