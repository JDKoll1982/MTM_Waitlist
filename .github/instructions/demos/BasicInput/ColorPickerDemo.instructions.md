---
description: "ColorPicker (Microsoft.UI.Xaml.Controls.ColorPicker) in Basic input; a full colour chooser with spectrum, sliders and optional alpha, emitted… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/ColorPickerDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/ColorPickerDemo.xaml.cs"
---

# ColorPicker

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.ColorPicker` |
| Page | `BasicInput/ColorPickerDemo` (`WinUI3LearningLab.Demos.BasicInput.ColorPickerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo color-picker` |

## What it teaches

A full colour chooser with spectrum, sliders and optional alpha, emitted as a Windows.UI.Color.

## How the demo is built

The page is a `UserControl` at `BasicInput/ColorPickerDemo.xaml`, paired with `ColorPickerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 11 element(s).

Named elements the code-behind touches: `Picker`, `Swatch`, `HexText`, `ArgbText`.

Wired in XAML: `OnColorChanged()`.

**Code-behind.** Handlers `OnColorChanged()`. 32 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Color is Windows.UI.Color (not Microsoft.UI); use Microsoft.UI.ColorHelper or Windows.UI.Color.FromArgb to build one.
- IsAlphaEnabled is false by default, so the alpha slider is invisible until you turn it on.
- ColorChanged fires continuously while dragging — throttle expensive work instead of doing it per event.

## Traps recorded in the source

- Set in code, not XAML: a literal colour string cannot be assigned to ColorPicker.Color at runtime in this app (E_INVALIDARG 'Failed to assign to property Color'), so the initial selection is applied here instead.

## Try this

- Run `WinUI3LearningLab.App.exe --demo color-picker`. The page shows `ColorPickerDemo.xaml` and `ColorPickerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ColorPicker](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.colorpicker)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
