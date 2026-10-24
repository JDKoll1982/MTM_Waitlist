---
description: "SplitButton (Microsoft.UI.Xaml.Controls.SplitButton) in Basic input; two hit targets in one control: a default action and a chevron that opens… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/SplitButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/SplitButtonDemo.xaml.cs"
---

# SplitButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.SplitButton` |
| Page | `BasicInput/SplitButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.SplitButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo split-button` |

## What it teaches

Two hit targets in one control: a default action and a chevron that opens related commands.

## How the demo is built

The page is a `UserControl` at `BasicInput/SplitButtonDemo.xaml`, paired with `SplitButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 16 element(s).

Named elements the code-behind touches: `StatusText`.

Wired in XAML: `OnSaveClicked()`, `OnMenuItemClicked()`.

**Code-behind.** Handlers `OnSaveClicked()`, `OnMenuItemClicked()`. 24 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Click fires only for the default-action half; the chevron opens Flyout and nothing else.
- You must supply the Flyout yourself — SplitButton does not create a menu.
- Set Command on the button for MVVM; the flyout items then carry their own commands.

## Try this

- Run `WinUI3LearningLab.App.exe --demo split-button`. The page shows `SplitButtonDemo.xaml` and `SplitButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.SplitButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.splitbutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
