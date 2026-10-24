---
description: "Ink (InkCanvas is UWP-only) (Windows.UI.Input.Inking.InkPresenter) in Media; WinUI 3 has no InkCanvas or InkToolbar; ink is drawn with… MTM: MTM_Receiving_Application › Receiving › label and photo previews."
applyTo: "src/WinUI3LearningLab.App/Demos/Media/InkCanvasDemo.xaml,src/WinUI3LearningLab.App/Demos/Media/InkCanvasDemo.xaml.cs"
---

# Ink (InkCanvas is UWP-only)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Media |
| Level | Advanced |
| API | `Windows.UI.Input.Inking.InkPresenter` |
| Page | `Media/InkCanvasDemo` (`WinUI3LearningLab.Demos.Media.InkCanvasDemo`) |
| Kind | Explanatory page |
| Run it | `WinUI3LearningLab.App.exe --demo ink-canvas` |

## What it teaches

WinUI 3 has no InkCanvas or InkToolbar; ink is drawn with pointer input into a Canvas, or with Win2D.

## Availability

Interactive, but reduced: the real InkCanvas/InkToolbar controls are not in Windows App SDK 2.3.1, so this page demonstrates pointer-driven drawing instead.

## How the demo is built

The page is a `UserControl` at `Media/InkCanvasDemo.xaml`, paired with `InkCanvasDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `ColorBox`, `WidthSlider`, `Surface`, `StrokeText`.

Wired in XAML: `OnStrokeSettingsChanged()`, `OnWidthChanged()`, `OnPointerPressed()`, `OnPointerMoved()`, `OnPointerReleased()`, `OnPointerCaptureLost()`, `OnClearClicked()`.

**Code-behind.** Handlers `OnPointerPressed()`, `OnPointerMoved()`, `OnPointerReleased()`, `OnPointerCaptureLost()`, `OnStrokeSettingsChanged()`, `OnWidthChanged()`, `OnClearClicked()`. 114 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › label and photo previews**

Labels and part photos are shown before they are printed, and a scanner camera is used where a handheld one is not. Media that fails to load must degrade to a placeholder rather than collapsing the row.

Schema this mirrors: `config_images_locations, dunnage_label_data, dunnage_history`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Microsoft.UI.Xaml.Controls.InkCanvas and InkToolbar do not exist in this SDK — verified against the reference assembly.
- For a real inking surface use Windows.UI.Input.Inking.InkPresenter with a Win2D CanvasControl, or Microsoft.Graphics.Win2D.
- This page shows the portable technique: capture PointerPressed/Moved/Released and build a Polyline, which is enough for annotation.

## Try this

- Run `WinUI3LearningLab.App.exe --demo ink-canvas`. The page shows `InkCanvasDemo.xaml` and `InkCanvasDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Windows.UI.Input.Inking.InkPresenter](https://learn.microsoft.com/windows/apps/design/input/pen-and-stylus-interactions)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Media.cs` or the demo source, then regenerate.
