---
description: "Chart (no framework control) (Concept only) in Media; neither WinUI 3 nor the Community Toolkit ships a chart control; charts are… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/ChartDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/ChartDemo.xaml.cs"
---

# Chart (no framework control)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Intermediate |
| API | `Concept only` |
| Page | `Media/ChartDemo` (`WinUI3LearningLab.Demos.Media.ChartDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo chart` |

## What it teaches

Neither WinUI 3 nor the Community Toolkit ships a chart control; charts are composed from primitives.

## Availability

Documentation-only page: this SDK has no chart control. The page shows a hand-rolled bar chart built from Border primitives.

## How the demo is built

The page is a `UserControl` at `Media/ChartDemo.xaml`, paired with `ChartDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 70 element(s).

Named elements the code-behind touches: `ChartTitle`, `LegendSwatch`, `LegendText`, `ValueAxis`, `AxisFull`, `AxisThreeQuarter`, `AxisHalf`, `AxisQuarter`, `BarLayer`, `MonthLayer`, `SummaryText`, `MetricBox`, `Rows`.

Wired in XAML: `OnMetricChanged()`.

**Code-behind.** Handlers `OnMetricChanged()`. 135 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- There is no Chart in Microsoft.UI.Xaml.Controls and no chart in the WinUI 3 Community Toolkit — the Gallery sample is its own implementation.
- Practical options: shapes over a Grid, ItemsRepeater with UniformGridLayout, Win2D, or a third-party package such as LiveCharts2 or Syncfusion.
- For accessibility, pair every chart with a text or table equivalent; a canvas of rectangles reads as nothing to a screen reader.

## Try this

- Run `WinUI3LearningLab.App.exe --demo chart`. The page shows `ChartDemo.xaml` and `ChartDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Concept only](https://github.com/microsoft/WinUI-Gallery)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
