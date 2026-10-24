---
description: "TreeView (Microsoft.UI.Xaml.Controls.TreeView) in Collections; hierarchical list with expand/collapse, drag-and-drop and an optional checkbox… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/TreeViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/TreeViewDemo.xaml.cs"
---

# TreeView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.TreeView` |
| Page | `Collections/TreeViewDemo` (`WinUI3LearningLab.Demos.Collections.TreeViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo tree-view` |

## What it teaches

Hierarchical list with expand/collapse, drag-and-drop and an optional checkbox model.

## How the demo is built

The page is a `UserControl` at `Collections/TreeViewDemo.xaml`, paired with `TreeViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `BinTree`, `SelectionText`.

Wired in XAML: `OnTreeSelectionChanged()`, `OnExpandAll()`, `OnCollapseAll()`, `OnSelectFirstBin()`.

**Code-behind.** Handlers `OnTreeSelectionChanged()`, `OnExpandAll()`, `OnCollapseAll()`, `OnSelectFirstBin()`. 120 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Nodes are TreeViewNode objects in RootNodes, not items — binding ItemsSource requires TreeViewItemTemplate and a flat data source with children.
- Expanding a node raises Expanding, which is where lazy-loaded children are added.
- SelectionMode can be Single, Multiple or None; check-boxes are a separate feature controlled by the item template.

## Try this

- Run `WinUI3LearningLab.App.exe --demo tree-view`. The page shows `TreeViewDemo.xaml` and `TreeViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.TreeView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.treeview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
