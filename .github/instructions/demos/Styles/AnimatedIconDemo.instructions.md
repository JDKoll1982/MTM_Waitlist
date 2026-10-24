---
description: "AnimatedIcon (Microsoft.UI.Xaml.Controls.AnimatedIcon) in Styles & brushes; an icon that animates between states, driven by a visual state group… MTM: MTM_WIP_Application_WinForms › Every form › theme."
applyTo: "src/WinUI3LearningLab.App/Demos/Styles/AnimatedIconDemo.xaml,src/WinUI3LearningLab.App/Demos/Styles/AnimatedIconDemo.xaml.cs"
---

# AnimatedIcon

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Styles & brushes |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.AnimatedIcon` |
| Page | `Styles/AnimatedIconDemo` (`WinUI3LearningLab.Demos.Styles.AnimatedIconDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo animated-icon` |

## What it teaches

An icon that animates between states, driven by a visual state group and an IAnimatedVisualSource.

## How the demo is built

The page is a `UserControl` at `Styles/AnimatedIconDemo.xaml`, paired with `AnimatedIconDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `NavButton`, `NavIcon`, `StateText`.

Wired in XAML: `OnNavPointerEntered()`, `OnNavPointerExited()`, `OnNavPointerPressed()`, `OnNavPointerReleased()`, `OnSetNormalClicked()`, `OnSetPointerOverClicked()`, `OnSetPressedClicked()`.

**Code-behind.** Handlers `OnNavPointerEntered()`, `OnNavPointerExited()`, `OnNavPointerPressed()`, `OnNavPointerReleased()`, `OnSetNormalClicked()`, `OnSetPointerOverClicked()`, `OnSetPressedClicked()`. 35 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › theme**

Colours come from a theme row in the database because the app runs under shop-floor lighting on different monitors. A literal hex value in a page is what makes a form unreadable in the other theme, so brushes are named and resolved from resources.

Schema this mirrors: `app_themes, sys_visual`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The built-in sources (Microsoft.UI.Xaml.Controls.AnimatedVisuals) follow specific state names such as Normal, PointerOver and Pressed.
- Without a Source the control renders nothing at all — it does not fall back to a static glyph.
- AnimatedIcon is decorative: pair it with text or an AutomationProperties.Name.

## Traps recorded in the source

- Still inside the button, so the release goes back to hover rather than all the way to Normal.

## Try this

- Run `WinUI3LearningLab.App.exe --demo animated-icon`. The page shows `AnimatedIconDemo.xaml` and `AnimatedIconDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.AnimatedIcon](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.animatedicon)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Styles.cs` or the demo source, then regenerate.
