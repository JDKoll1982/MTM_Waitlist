---
description: "ToggleButton (Microsoft.UI.Xaml.Controls.Primitives.ToggleButton) in Basic input; a two-state button. It is the base class for CheckBox and… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/ToggleButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/ToggleButtonDemo.xaml.cs"
---

# ToggleButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Primitives.ToggleButton` |
| Page | `BasicInput/ToggleButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.ToggleButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo toggle-button` |

## What it teaches

A two-state button. It is the base class for CheckBox and RadioButton and is the right control for a toolbar toggle.

## How the demo is built

The page is a `UserControl` at `BasicInput/ToggleButtonDemo.xaml`, paired with `ToggleButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `BoldToggle`, `ItalicToggle`, `PreviewText`, `ThreeStateText`.

Wired in XAML: `OnToggleChanged()`, `OnThreeStateChanged()`.

**Code-behind.** Handlers `OnToggleChanged()`, `OnThreeStateChanged()`. 32 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Exposes IsChecked (a nullable bool) plus Checked/Unchecked/Indeterminate events; IsThreeState adds the third value.
- IsChecked is bool? — compare with == true rather than relying on implicit conversion in bindings.
- Prefer ToggleSwitch when the toggle is a setting with a label; ToggleButton is for compact toolbars.

## Try this

- Run `WinUI3LearningLab.App.exe --demo toggle-button`. The page shows `ToggleButtonDemo.xaml` and `ToggleButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Primitives.ToggleButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.togglebutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
