---
description: "Line (Microsoft.UI.Xaml.Shapes.Line) in Styles & brushes; a straight line between two points, with stroke, dash pattern and caps. MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/LineDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/LineDemo.xaml.cs"
---

# Line

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Shapes.Line` |
| Page | `Styles/LineDemo` (`WinUI3LearningLab.Demos.Styles.LineDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo line` |

## What it teaches

A straight line between two points, with stroke, dash pattern and caps.

## How the demo is built

The page is a `UserControl` at `Styles/LineDemo.xaml`, paired with `LineDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 29 element(s).

Named elements the code-behind touches: `ChartCanvas`, `ThicknessSlider`, `DashSlider`, `StatusText`.

Wired in XAML: `OnThicknessChanged()`, `OnDashChanged()`.

**Code-behind.** Handlers `OnLoaded()`, `OnCanvasSizeChanged()`, `OnThicknessChanged()`, `OnDashChanged()`. 239 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- X1/Y1/X2/Y2 are in the coordinate space of the parent, so a Line inside a StackPanel is positioned absolutely.
- StrokeDashArray is measured in multiples of StrokeThickness, not pixels.
- One DoubleCollection cannot be assigned to two Shapes: the second assignment throws E_INVALIDARG 0x80070057. Build a fresh collection per Line — verified on Windows App SDK 2.3.1, and null or empty is accepted, so the failure is the sharing, not the value.
- For separators prefer a 1px Border or Rectangle that stretches; a Line does not participate in layout.

## Traps recorded in the source

- SizeChanged fires during the layout pass. Clearing and re-adding children from inside it re-enters layout, and the process dies with a stowed exception (0xC000027B) rather than a catchable error. The rebuild is therefore deferred to the dispatcher, once per burst.
- A fresh collection per Line: one DoubleCollection cannot be shared between Shapes.

## Try this

- Run `WinUI3LearningLab.App.exe --demo line`. The page shows `LineDemo.xaml` and `LineDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Shapes.Line](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.shapes.line)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
