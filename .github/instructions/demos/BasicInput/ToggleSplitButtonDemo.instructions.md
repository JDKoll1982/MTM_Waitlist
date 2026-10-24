---
description: "ToggleSplitButton (Microsoft.UI.Xaml.Controls.ToggleSplitButton) in Basic input; a SplitButton whose primary half is a toggle, for things like… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/ToggleSplitButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/ToggleSplitButtonDemo.xaml.cs"
---

# ToggleSplitButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.ToggleSplitButton` |
| Page | `BasicInput/ToggleSplitButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.ToggleSplitButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo toggle-split-button` |

## What it teaches

A SplitButton whose primary half is a toggle, for things like 'bold' with a menu of related formatting.

## How the demo is built

The page is a `UserControl` at `BasicInput/ToggleSplitButtonDemo.xaml`, paired with `ToggleSplitButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `BoldButton`, `PreviewText`, `StatusText`.

Wired in XAML: `OnToggleClicked()`, `OnIsCheckedChanged()`, `OnMenuClicked()`.

**Code-behind.** Handlers `OnToggleClicked()`, `OnIsCheckedChanged()`, `OnMenuClicked()`. 45 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- IsChecked is a bool (not bool?) here, unlike ToggleButton.
- IsCheckedChanged fires before your Click handler; read the final state there rather than from Click.
- The menu half does not change IsChecked — that would be surprising to keyboard users.

## Try this

- Run `WinUI3LearningLab.App.exe --demo toggle-split-button`. The page shows `ToggleSplitButtonDemo.xaml` and `ToggleSplitButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ToggleSplitButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.togglesplitbutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
