---
description: "RadialGradientBrush (Microsoft.UI.Xaml.Media.RadialGradientBrush) in Styles & brushes; a circular gradient with controllable centre, origin and… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/RadialGradientBrushDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/RadialGradientBrushDemo.xaml.cs"
---

# RadialGradientBrush

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Media.RadialGradientBrush` |
| Page | `Styles/RadialGradientBrushDemo` (`WinUI3LearningLab.Demos.Styles.RadialGradientBrushDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo radial-gradient-brush` |

## What it teaches

A circular gradient with controllable centre, origin and spread — WinUI 3 only, unlike UWP.

## How the demo is built

The page is a `UserControl` at `Styles/RadialGradientBrushDemo.xaml`, paired with `RadialGradientBrushDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 25 element(s).

Named elements the code-behind touches: `BrushHost`, `Glow`, `MarkerCanvas`, `CenterMarker`, `OriginMarker`, `CenterXSlider`, `CenterYSlider`, `OriginXSlider`, `OriginYSlider`, `MappingSwitch`, `StatusText`.

Wired in XAML: `OnGeometryChanged()`, `OnMappingToggled()`.

**Code-behind.** Handlers `OnLoaded()`, `OnBrushHostSizeChanged()`, `OnGeometryChanged()`, `OnMappingToggled()`. 100 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It does not exist in UWP XAML: this is a Windows App SDK addition, so copied UWP samples will not build.
- GradientOrigin offsets where the gradient starts inside the ellipse; setting it equal to Center makes a symmetric glow.
- MappingMode=Absolute switches the coordinates from relative (0-1) to pixels, which changes how it scales.

## Try this

- Run `WinUI3LearningLab.App.exe --demo radial-gradient-brush`. The page shows `RadialGradientBrushDemo.xaml` and `RadialGradientBrushDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.RadialGradientBrush](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.radialgradientbrush)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
