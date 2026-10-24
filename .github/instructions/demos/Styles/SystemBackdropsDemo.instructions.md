---
description: "System backdrops (Mica / Acrylic) (Microsoft.UI.Composition.SystemBackdrops.MicaController) in Styles & brushes; the window-level materials — Mica (desktop wallpaper tint)… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/SystemBackdropsDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/SystemBackdropsDemo.xaml.cs"
---

# System backdrops (Mica / Acrylic)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Intermediate |
| API | `Microsoft.UI.Composition.SystemBackdrops.MicaController` |
| Page | `Styles/SystemBackdropsDemo` (`WinUI3LearningLab.Demos.Styles.SystemBackdropsDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo system-backdrops` |

## What it teaches

The window-level materials — Mica (desktop wallpaper tint) and Desktop Acrylic — applied to a WinUI 3 window.

## How the demo is built

The page is a `UserControl` at `Styles/SystemBackdropsDemo.xaml`, paired with `SystemBackdropsDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `StatusText`, `SupportText`.

Wired in XAML: `OnMicaClicked()`, `OnMicaAltClicked()`, `OnAcrylicClicked()`, `OnNoneClicked()`.

**Code-behind.** Handlers `OnMicaClicked()`, `OnMicaAltClicked()`, `OnAcrylicClicked()`, `OnNoneClicked()`. 53 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In WinUI 3 you can simply assign Window.SystemBackdrop = new MicaBackdrop(); the controller types are for Composition-level control.
- Mica requires Windows 11; on Windows 10 the assignment silently does nothing, so keep a solid fallback colour.
- A backdrop shows only where the app content is transparent — opaque backgrounds hide it completely.

## Try this

- Run `WinUI3LearningLab.App.exe --demo system-backdrops`. The page shows `SystemBackdropsDemo.xaml` and `SystemBackdropsDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Composition.SystemBackdrops.MicaController](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.systembackdrops.micacontroller)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
