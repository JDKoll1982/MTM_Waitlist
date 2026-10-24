---
description: "Viewbox (Microsoft.UI.Xaml.Controls.Viewbox) in Layout; scales a single child uniformly to fit the available space… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/ViewboxDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/ViewboxDemo.xaml.cs"
---

# Viewbox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Viewbox` |
| Page | `Layout/ViewboxDemo` (`WinUI3LearningLab.Demos.Layout.ViewboxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo viewbox` |

## What it teaches

Scales a single child uniformly to fit the available space, preserving aspect ratio.

## How the demo is built

The page is a `UserControl` at `Layout/ViewboxDemo.xaml`, paired with `ViewboxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 20 element(s).

Named elements the code-behind touches: `StretchBox`, `HeightSlider`, `DiagramBox`, `Scaler`, `StretchText`.

Wired in XAML: `OnStretchChanged()`, `OnHeightChanged()`.

**Code-behind.** Handlers `OnStretchChanged()`, `OnHeightChanged()`. 60 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- One child only, and the child is measured with infinite space — text inside a Viewbox can scale to unreadable sizes.
- Stretch=Uniform keeps the aspect ratio; Fill distorts it, which is rarely what you want.
- Viewbox scaling is a render transform: hit-testing and pointer coordinates are transformed with it.

## Try this

- Run `WinUI3LearningLab.App.exe --demo viewbox`. The page shows `ViewboxDemo.xaml` and `ViewboxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Viewbox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.viewbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
