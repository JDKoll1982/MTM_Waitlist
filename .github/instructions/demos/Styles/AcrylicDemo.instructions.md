---
description: "AcrylicBrush (Microsoft.UI.Xaml.Media.AcrylicBrush) in Styles & brushes; the translucent, blurred material used for flyouts and sidebars… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/AcrylicDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/AcrylicDemo.xaml.cs"
---

# AcrylicBrush

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Media.AcrylicBrush` |
| Page | `Styles/AcrylicDemo` (`WinUI3LearningLab.Demos.Styles.AcrylicDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo acrylic-brush` |

## What it teaches

The translucent, blurred material used for flyouts and sidebars, controlled by tint, opacity and luminosity.

## How the demo is built

The page is a `UserControl` at `Styles/AcrylicDemo.xaml`, paired with `AcrylicDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 28 element(s).

Named elements the code-behind touches: `BackdropBrush`, `BackdropStopA`, `BackdropStopB`, `BackdropStopC`, `DefaultPanel`, `DefaultTintText`, `BasePanel`, `BaseTintText`, `BrightnessSlider`.

Wired in XAML: `OnBrightnessChanged()`.

**Code-behind.** Handlers `OnLoaded()`, `OnBrightnessChanged()`. 61 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Acrylic samples what is behind the window, not just behind the element — with a transparent window the result is very different.
- Use the theme resources (AcrylicBackgroundFillColorDefaultBrush) rather than constructing brushes, so light/dark/high-contrast all work.
- TintOpacity is the main knob; setting TintColor without changing opacity usually looks like a solid colour.

## Try this

- Run `WinUI3LearningLab.App.exe --demo acrylic-brush`. The page shows `AcrylicDemo.xaml` and `AcrylicDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.AcrylicBrush](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.acrylicbrush)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
