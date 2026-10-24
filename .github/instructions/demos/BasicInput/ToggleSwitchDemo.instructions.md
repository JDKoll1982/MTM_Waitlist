---
description: "ToggleSwitch (Microsoft.UI.Xaml.Controls.ToggleSwitch) in Basic input; an immediate-action on/off switch. Use it for settings that apply the… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/ToggleSwitchDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/ToggleSwitchDemo.xaml.cs"
---

# ToggleSwitch

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.ToggleSwitch` |
| Page | `BasicInput/ToggleSwitchDemo` (`WinUI3LearningLab.Demos.BasicInput.ToggleSwitchDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo toggle-switch` |

## What it teaches

An immediate-action on/off switch. Use it for settings that apply the moment they are flipped.

## How the demo is built

The page is a `UserControl` at `BasicInput/ToggleSwitchDemo.xaml`, paired with `ToggleSwitchDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `WifiSwitch`, `RichSwitch`, `StatusText`.

Wired in XAML: `OnToggled()`.

**Code-behind.** Handlers `OnToggled()`. 23 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Unlike CheckBox, the change is expected to take effect immediately — do not use it as a 'submit later' checkbox.
- OffContent and OnContent are objects, so they can hold icons or small layouts.
- Toggled fires once per change; do not also handle the pointer events or the work happens twice.

## Traps recorded in the source

- Set in code, not XAML: a literal on ToggleSwitch.IsOn fails at parse time in this app.

## Try this

- Run `WinUI3LearningLab.App.exe --demo toggle-switch`. The page shows `ToggleSwitchDemo.xaml` and `ToggleSwitchDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ToggleSwitch](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.toggleswitch)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
