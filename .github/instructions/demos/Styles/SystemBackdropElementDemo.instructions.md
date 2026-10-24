---
description: "SystemBackdropElement (Microsoft.UI.Xaml.Controls.SystemBackdropElement) in Styles & brushes; applies a system backdrop to a region inside the window… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/SystemBackdropElementDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/SystemBackdropElementDemo.xaml.cs"
---

# SystemBackdropElement

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Advanced |
| API | `Microsoft.UI.Xaml.Controls.SystemBackdropElement` |
| Page | `Styles/SystemBackdropElementDemo` (`WinUI3LearningLab.Demos.Styles.SystemBackdropElementDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo system-backdrop-element` |

## What it teaches

Applies a system backdrop to a region inside the window instead of the whole window.

## How the demo is built

The page is a `UserControl` at `Styles/SystemBackdropElementDemo.xaml`, paired with `SystemBackdropElementDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `BackdropRegion`, `RegionText`, `KindSwitch`.

Wired in XAML: `OnKindToggled()`.

**Code-behind.** Handlers `OnLoaded()`, `OnKindToggled()`. 45 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The element needs a non-opaque parent chain to show through; a solid parent background defeats it.
- It is a newer control — check the Windows App SDK version before relying on it.
- Use it for sidebars and panels; the window-level SystemBackdrop is still the cheaper choice for the whole window.

## Try this

- Run `WinUI3LearningLab.App.exe --demo system-backdrop-element`. The page shows `SystemBackdropElementDemo.xaml` and `SystemBackdropElementDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.SystemBackdropElement](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.systembackdropelement)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
