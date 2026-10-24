---
description: "DropShadowPanel (CommunityToolkit.WinUI.UI.Controls.DropShadowPanel) in Controls; wraps content in a Composition drop shadow, with blur… MTM: MTM_WIP_Application_WinForms › Inventory › grid and settings."
applyTo: "src/WinUI3LearningLab.App/Demos/Toolkit/DropShadowPanelDemo.xaml,src/WinUI3LearningLab.App/Demos/Toolkit/DropShadowPanelDemo.xaml.cs"
---

# DropShadowPanel

| | |
| --- | --- |
| Ecosystem | Community Toolkit 7.x |
| Category | Controls |
| Level | Intermediate |
| API | `CommunityToolkit.WinUI.UI.Controls.DropShadowPanel` |
| Page | `Toolkit/DropShadowPanelDemo` (`WinUI3LearningLab.Demos.Toolkit.DropShadowPanelDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo drop-shadow-panel` |

## What it teaches

Wraps content in a Composition drop shadow, with blur radius, colour, opacity and offset.

## How the demo is built

The page is a `UserControl` at `Toolkit/DropShadowPanelDemo.xaml`, paired with `DropShadowPanelDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 17 element(s).

Named elements the code-behind touches: `ShadowPanel`, `BlurSlider`, `OpacitySlider`, `OffsetXSlider`, `OffsetYSlider`, `ValuesText`, `LastActionText`.

Wired in XAML: `OnBlurChanged()`, `OnOpacityChanged()`, `OnOffsetXChanged()`, `OnOffsetYChanged()`.

**Code-behind.** Handlers `OnBlurChanged()`, `OnOpacityChanged()`, `OnOffsetXChanged()`, `OnOffsetYChanged()`. 44 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › grid and settings**

The grid remembers each user's column order and width, and the settings screen is a list of rows with a control on the right. Those two shapes — a data grid and a settings row — are the reason the Community Toolkit is a dependency of the real apps at all.

Schema this mirrors: `inv_inventory, usr_dgv_settings, settings_personal, md_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The shadow is drawn with Composition, so it needs a loaded visual — setting properties in the constructor has no effect.
- A shadow around clipped content is clipped too; leave padding inside the panel for the blur to breathe.
- For Fluent elevation prefer ThemeShadow or the standard card brushes; custom shadows drift from the design system.

## Try this

- Run `WinUI3LearningLab.App.exe --demo drop-shadow-panel`. The page shows `DropShadowPanelDemo.xaml` and `DropShadowPanelDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [CommunityToolkit.WinUI.UI.Controls.DropShadowPanel](https://learn.microsoft.com/dotnet/communitytoolkit/windows/)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Toolkit.cs` or the demo source, then regenerate.
