---
description: "DropDownButton (Microsoft.UI.Xaml.Controls.DropDownButton) in Basic input; a button with a chevron that opens a flyout of commands. Use it… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/DropDownButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/DropDownButtonDemo.xaml.cs"
---

# DropDownButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.DropDownButton` |
| Page | `BasicInput/DropDownButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.DropDownButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo drop-down-button` |

## What it teaches

A button with a chevron that opens a flyout of commands. Use it when the actions are a list rather than one command.

## How the demo is built

The page is a `UserControl` at `BasicInput/DropDownButtonDemo.xaml`, paired with `DropDownButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 20 element(s).

Named elements the code-behind touches: `StatusText`.

Wired in XAML: `OnExportClicked()`.

**Code-behind.** Handlers `OnExportClicked()`. 19 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

**Split** on the receiving screen posts with the remembered printer, while the chevron opens the other printers. A split button is right when one choice is overwhelmingly the default.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The button itself raises no Click command of its own; the work happens on the MenuFlyoutItem commands.
- Attach the flyout with the Flyout property; a MenuFlyout is the idiomatic choice because it gets keyboard support for free.
- Unlike SplitButton, the whole button surface opens the menu — there is no separate default action.

## Try this

- Run `WinUI3LearningLab.App.exe --demo drop-down-button`. The page shows `DropDownButtonDemo.xaml` and `DropDownButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.DropDownButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.dropdownbutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
