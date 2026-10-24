---
description: "Canvas (Microsoft.UI.Xaml.Controls.Canvas) in Layout; absolute positioning by attached Left/Top coordinates. It ignores… MTM: MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl."
applyTo: "src/WinUI3LearningLab.App/Demos/Layout/CanvasDemo.xaml,src/WinUI3LearningLab.App/Demos/Layout/CanvasDemo.xaml.cs"
---

# Canvas

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Layout |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Canvas` |
| Page | `Layout/CanvasDemo` (`WinUI3LearningLab.Demos.Layout.CanvasDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo canvas` |

## What it teaches

Absolute positioning by attached Left/Top coordinates. It ignores the size of its children when measuring.

## How the demo is built

The page is a `UserControl` at `Layout/CanvasDemo.xaml`, paired with `CanvasDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `Board`, `MovingChip`, `ChipCoordinates`, `BoardSizeText`, `CoordinateText`, `LeftSlider`.

Wired in XAML: `OnAnimateClicked()`, `OnResetClicked()`, `OnLeftChanged()`.

**Code-behind.** Handlers `OnAnimateClicked()`, `OnResetClicked()`, `OnLeftChanged()`. 87 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Every form › ThemedForm / ThemedUserControl**

All 68 designer forms are laid out on the same surface: a header, a scrolling body and a footer of actions. The lab rebuilds that shape with panels and star sizing so it survives 150% scaling on a 16-inch laptop instead of the 4K monitor it was drawn on.

Schema this mirrors: `app_themes, usr_dgv_settings`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Left/Top are attached properties from Canvas, not members of the child.
- Canvas never clips by default and never reports a useful DesiredSize — give it an explicit size or it collapses.
- It is the tool for diagrams and overlays, not for ordinary layout: nothing reflows when the window resizes.

## Try this

- Run `WinUI3LearningLab.App.exe --demo canvas`. The page shows `CanvasDemo.xaml` and `CanvasDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Canvas](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.canvas)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Layout.cs` or the demo source, then regenerate.
