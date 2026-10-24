---
description: "ThemeShadow (Microsoft.UI.Xaml.Media.ThemeShadow) in Styles & brushes; depth shadows that respond to the theme and to the elevation of the… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/ThemeShadowDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/ThemeShadowDemo.xaml.cs"
---

# ThemeShadow

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Media.ThemeShadow` |
| Page | `Styles/ThemeShadowDemo` (`WinUI3LearningLab.Demos.Styles.ThemeShadowDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo theme-shadow` |

## What it teaches

Depth shadows that respond to the theme and to the elevation of the surrounding elements.

## How the demo is built

The page is a `UserControl` at `Styles/ThemeShadowDemo.xaml`, paired with `ThemeShadowDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 26 element(s).

Named elements the code-behind touches: `FlatCard`, `RaisedCard`, `ElevationText`.

Wired in XAML: `OnToggleElevation()`.

**Code-behind.** Handlers `OnToggleElevation()`. 40 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- A shadow needs receivers: set UIElement.Shadow and the element must be raised on the Z axis with Translation to be visible.
- ThemeShadow is not a DropShadow — for a simple blurred shadow on arbitrary content the Toolkit's DropShadowPanel is easier.
- Shadows in a flyout or popup need the popup's own receiver registration, which is why their shadows look different.

## Try this

- Run `WinUI3LearningLab.App.exe --demo theme-shadow`. The page shows `ThemeShadowDemo.xaml` and `ThemeShadowDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Media.ThemeShadow](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.themeshadow)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
