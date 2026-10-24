---
description: "GridSplitter (CommunityToolkit.WinUI.UI.Controls.GridSplitter) in Controls; a draggable divider that resizes Grid rows or columns at… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/GridSplitterDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/GridSplitterDemo.xaml.cs"
---

# GridSplitter

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Intermediate |
| API | `CommunityToolkit.WinUI.UI.Controls.GridSplitter` |
| Page | `Toolkit/GridSplitterDemo` (`WinUI3LearningLab.Demos.Toolkit.GridSplitterDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo grid-splitter` |

## What it teaches

A draggable divider that resizes Grid rows or columns at runtime.

## How the demo is built

The page is a `UserControl` at `Toolkit/GridSplitterDemo.xaml`, paired with `GridSplitterDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 24 element(s).

Named elements the code-behind touches: `LeftPane`, `RightPane`, `WidthText`, `LastActionText`.

Wired in XAML: `OnPaneSizeChanged()`.

**Code-behind.** Handlers `OnPaneSizeChanged()`. 30 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It only resizes star-sized rows or columns; Auto and fixed sizes snap back.
- Place it in its own row/column, or set ResizeBehavior/ResizeDirection, otherwise it fights the layout.
- Keyboard users need a focusable splitter — the toolkit control supports arrow keys when focused.

## Try this

- Run `WinUI3LearningLab.App.exe --demo grid-splitter`. The page shows `GridSplitterDemo.xaml` and `GridSplitterDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.GridSplitter](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
