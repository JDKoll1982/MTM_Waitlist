---
description: "CheckBox (Microsoft.UI.Xaml.Controls.CheckBox) in Basic input; an independent on/off choice. Use CheckBox for multiple selections and… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/CheckBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/CheckBoxDemo.xaml.cs"
---

# CheckBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.CheckBox` |
| Page | `BasicInput/CheckBoxDemo` (`WinUI3LearningLab.Demos.BasicInput.CheckBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo check-box` |

## What it teaches

An independent on/off choice. Use CheckBox for multiple selections and RadioButton when exactly one applies.

## How the demo is built

The page is a `UserControl` at `BasicInput/CheckBoxDemo.xaml`, paired with `CheckBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 7 element(s).

Named elements the code-behind touches: `TelemetryCheck`, `StatusText`.

Wired in XAML: `OnCheckChanged()`, `OnThreeStateChanged()`.

**Code-behind.** Handlers `OnCheckChanged()`, `OnThreeStateChanged()`. 32 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- IsChecked is bool? and IsThreeState enables the indeterminate state used for 'mixed' parent settings.
- The clickable area includes the label, so keep Content short and put long explanations in a separate TextBlock.
- Checked and Unchecked do not fire in a predictable order during initialization — do not rely on them for setup.

## Try this

- Run `WinUI3LearningLab.App.exe --demo check-box`. The page shows `CheckBoxDemo.xaml` and `CheckBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.CheckBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.checkbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
