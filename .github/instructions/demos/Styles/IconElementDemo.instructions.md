---
description: "IconElement (Microsoft.UI.Xaml.Controls.IconElement) in Styles & brushes; the base type for icons: FontIcon, SymbolIcon, BitmapIcon, PathIcon… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/IconElementDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/IconElementDemo.xaml.cs"
---

# IconElement

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.IconElement` |
| Page | `Styles/IconElementDemo` (`WinUI3LearningLab.Demos.Styles.IconElementDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo icon-element` |

## What it teaches

The base type for icons: FontIcon, SymbolIcon, BitmapIcon, PathIcon and ImageIcon all derive from it.

## How the demo is built

The page is a `UserControl` at `Styles/IconElementDemo.xaml`, paired with `IconElementDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 58 element(s).

Named elements the code-behind touches: `BitmapIconSample`, `MonoBitmapIconSample`, `ImageIconSample`, `IconHost`, `SizedFontIcon`, `SizeSlider`, `IconColorPicker`, `InheritText`, `AssetText`.

Wired in XAML: `OnSizeChanged()`, `OnColorChanged()`.

**Code-behind.** Handlers `OnLoaded()`, `OnSizeChanged()`, `OnColorChanged()`. 78 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- FontIcon needs a font that contains the glyph — the Fluent icons font is "Segoe Fluent Icons", and a missing glyph renders as a box.
- SymbolIcon only knows the small Symbol enum; it is convenient but limited compared with FontIcon.
- Icons inherit Foreground, so theme the parent rather than setting colours per icon.

## Traps recorded in the source

- A 32 x 32 PNG drawn by the lab itself: the app ships no image assets, and BitmapIcon only accepts a URI, so the demo materialises this one file before pointing the two bitmap icons at it.

## Try this

- Run `WinUI3LearningLab.App.exe --demo icon-element`. The page shows `IconElementDemo.xaml` and `IconElementDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.IconElement](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.iconelement)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
