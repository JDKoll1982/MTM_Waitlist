---
description: "ComboBox (Microsoft.UI.Xaml.Controls.ComboBox) in Basic input; a drop-down single-selection list. It is the compact choice when the option… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/ComboBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/ComboBoxDemo.xaml.cs"
---

# ComboBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ComboBox` |
| Page | `BasicInput/ComboBoxDemo` (`WinUI3LearningLab.Demos.BasicInput.ComboBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo combo-box` |

## What it teaches

A drop-down single-selection list. It is the compact choice when the option list is longer than a few items.

## How the demo is built

The page is a `UserControl` at `BasicInput/ComboBoxDemo.xaml`, paired with `ComboBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 9 element(s).

Named elements the code-behind touches: `ThemeBox`, `StatusText`.

Wired in XAML: `OnSelectionChanged()`.

**Code-behind.** Handlers `OnSelectionChanged()`. 29 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- SelectedItem is an object: with string items you get the string, with bound objects you get your object.
- IsEditable turns it into a text box with suggestions; the typing surface is TextSubmitted, not SelectionChanged.
- PlaceholderText only shows while nothing is selected — after a selection it is invisible.

## Try this

- Run `WinUI3LearningLab.App.exe --demo combo-box`. The page shows `ComboBoxDemo.xaml` and `ComboBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ComboBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.combobox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
