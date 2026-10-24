---
description: "SemanticZoom (Microsoft.UI.Xaml.Controls.SemanticZoom) in Scrolling; two views of the same data with pinch/ctrl-scroll zoom… MTM: MTM_WIP_Application_WinForms › Inventory › stock list and last transactions."
applyTo: "src/WinUI3LearningLab.App/Demos/Scrolling/SemanticZoomDemo.xaml,src/WinUI3LearningLab.App/Demos/Scrolling/SemanticZoomDemo.xaml.cs"
---

# SemanticZoom

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Scrolling |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.SemanticZoom` |
| Page | `Scrolling/SemanticZoomDemo` (`WinUI3LearningLab.Demos.Scrolling.SemanticZoomDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo semantic-zoom` |

## What it teaches

Two views of the same data with pinch/ctrl-scroll zoom between them — a full list and a grouped overview.

## How the demo is built

The page is a `UserControl` at `Scrolling/SemanticZoomDemo.xaml`, paired with `SemanticZoomDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 29 element(s).

Named elements the code-behind touches: `ZoomStatus`, `Zoom`, `DetailList`, `OverviewGrid`.

Wired in XAML: `OnToggleZoomClicked()`, `OnViewChangeCompleted()`.

**Code-behind.** Handlers `OnToggleZoomClicked()`, `OnViewChangeCompleted()`. 64 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › stock list and last transactions**

The stock list is tens of thousands of rows on a workstation that also runs the label printer. Realising rows lazily and keeping the header in view is the difference between a usable screen and a frozen one.

Schema this mirrors: `inv_inventory, sys_last_10_transactions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It needs two views whose data is related; a ListView plus a GridView over the same collection is the canonical pair.
- CollectionViewSource with IsSourceGrouped is what makes the zoomed-out view meaningful.
- The control is largely legacy: it targets touch pinch gestures and is rarely the right choice for new desktop apps.

## Try this

- Run `WinUI3LearningLab.App.exe --demo semantic-zoom`. The page shows `SemanticZoomDemo.xaml` and `SemanticZoomDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.SemanticZoom](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.semanticzoom)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Scrolling.cs` or the demo source, then regenerate.
