---
description: "PipsPager (Microsoft.UI.Xaml.Controls.PipsPager) in Scrolling; the row of dots used to show position in a small set of… MTM: MTM_WIP_Application_WinForms › Inventory › stock list and last transactions."
applyTo: "src/WinUI3LearningLab.App/Demos/Scrolling/PipsPagerDemo.xaml,src/WinUI3LearningLab.App/Demos/Scrolling/PipsPagerDemo.xaml.cs"
---

# PipsPager

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Scrolling |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.PipsPager` |
| Page | `Scrolling/PipsPagerDemo` (`WinUI3LearningLab.Demos.Scrolling.PipsPagerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo pips-pager` |

## What it teaches

The row of dots used to show position in a small set of pages, as seen in WinUI's own onboarding screens.

## How the demo is built

The page is a `UserControl` at `Scrolling/PipsPagerDemo.xaml`, paired with `PipsPagerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 30 element(s).

Named elements the code-behind touches: `OrientationBox`, `PipStatus`, `DocFlip`, `Pips`.

Wired in XAML: `OnOrientationChanged()`, `OnFlipSelectionChanged()`, `OnPipChanged()`.

**Code-behind.** Handlers `OnPipChanged()`, `OnFlipSelectionChanged()`, `OnOrientationChanged()`. 54 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › stock list and last transactions**

The stock list is tens of thousands of rows on a workstation that also runs the label printer. Realising rows lazily and keeping the header in view is the difference between a usable screen and a frozen one.

Schema this mirrors: `inv_inventory, sys_last_10_transactions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- PipsPager does not page anything: you handle SelectedIndexChanged and move the content yourself.
- NumberOfPages decides how many pips exist; leaving it zero shows only the arrows.
- VerticalOrientation turns the dots into a column for side-by-side carousels.

## Try this

- Run `WinUI3LearningLab.App.exe --demo pips-pager`. The page shows `PipsPagerDemo.xaml` and `PipsPagerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.PipsPager](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.pipspager)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Scrolling.cs` or the demo source, then regenerate.
