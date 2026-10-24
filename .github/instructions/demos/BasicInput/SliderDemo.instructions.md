---
description: "Slider (Microsoft.UI.Xaml.Controls.Slider) in Basic input; continuous or stepped value selection along a track, with optional tooltips and… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/SliderDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/SliderDemo.xaml.cs"
---

# Slider

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Slider` |
| Page | `BasicInput/SliderDemo` (`WinUI3LearningLab.Demos.BasicInput.SliderDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo slider` |

## What it teaches

Continuous or stepped value selection along a track, with optional tooltips and tick marks.

## How the demo is built

The page is a `UserControl` at `BasicInput/SliderDemo.xaml`, paired with `SliderDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `ContinuousSlider`, `ContinuousText`, `SteppedSlider`, `SteppedText`, `VerticalSlider`.

Wired in XAML: `OnContinuousChanged()`, `OnSteppedChanged()`.

**Code-behind.** Handlers `OnContinuousChanged()`, `OnSteppedChanged()`. 33 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ValueChanged fires continuously while dragging; use the pointer-released state or debounce if the handler is expensive.
- TickFrequency alone does nothing unless you also set the TickPlacement.
- SnapToTick makes values land on steps but leaves the raw value a double — round it before display.

## Traps recorded in the source

- Initial values are set here rather than in XAML: in this app a numeric literal on Slider.Value fails at parse time with E_INVALIDARG ('Failed to assign to property RangeBase.Value'), while the identical value assigned in code is accepted.

## Try this

- Run `WinUI3LearningLab.App.exe --demo slider`. The page shows `SliderDemo.xaml` and `SliderDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Slider](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.slider)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
