---
description: "Shape (Microsoft.UI.Xaml.Shapes.Shape) in Styles & brushes; the shape family — Rectangle, Ellipse, Polygon, Polyline, Path — plus the shared… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/ShapeDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/ShapeDemo.xaml.cs"
---

# Shape

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Shapes.Shape` |
| Page | `Styles/ShapeDemo` (`WinUI3LearningLab.Demos.Styles.ShapeDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo shape` |

## What it teaches

The shape family — Rectangle, Ellipse, Polygon, Polyline, Path — plus the shared Fill/Stroke model.

## How the demo is built

The page is a `UserControl` at `Styles/ShapeDemo.xaml`, paired with `ShapeDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 49 element(s).

Named elements the code-behind touches: `DemoRectangle`, `DemoEllipse`, `DemoPolygon`, `DemoPolyline`, `DemoPath`, `ThicknessSlider`, `FillSlider`, `StrokeColorPicker`, `SlotRectangle`, `OverflowText`.

Wired in XAML: `OnThicknessChanged()`, `OnFillChanged()`, `OnStrokeColorChanged()`.

**Code-behind.** Handlers `OnThicknessChanged()`, `OnFillChanged()`, `OnStrokeColorChanged()`. 81 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Shapes have no content and no children: Path with PathGeometry is how you build anything complex.
- Fill is the interior and Stroke is the outline; StrokeThickness is centred on the geometry, so it overflows the layout slot by half.
- Shape sizes are not constrained by the parent unless you use Stretch or explicit Width/Height.

## Traps recorded in the source

- Polyline is an open run of segments, so it stays unfilled on purpose.

## Try this

- Run `WinUI3LearningLab.App.exe --demo shape`. The page shows `ShapeDemo.xaml` and `ShapeDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Shapes.Shape](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.shapes.shape)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
