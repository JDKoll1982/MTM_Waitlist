---
description: "Button (Microsoft.UI.Xaml.Controls.Button) in Basic input; the primary command surface. Raises Click, or fires a bound ICommand when the user… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/ButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/ButtonDemo.xaml.cs"
---

# Button

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.Button` |
| Page | `BasicInput/ButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.ButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo button` |

## What it teaches

The primary command surface. Raises Click, or fires a bound ICommand when the user presses, taps or invokes it with the keyboard.

## How the demo is built

The page is a `UserControl` at `BasicInput/ButtonDemo.xaml`, paired with `ButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 21 element(s).

Named elements the code-behind touches: `ResultText`.

Wired in XAML: `OnStandardClicked()`.

**Code-behind.** Handlers `OnStandardClicked()`. 23 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

On the receiving entry screen the single **Post receipt** action is the accent button; everything beside it is standard. The two disabled buttons mirror the state before a dunnage type is chosen — the button is disabled, not hidden, so the operator can see that posting exists and why it is unavailable.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Content is an object, not a string: a Button can hold an icon, a StackPanel, or any UIElement.
- Click does not fire while IsEnabled is false, and a disabled button is skipped by tab navigation.
- WinUI's Button has no IsDefault or IsCancel — pressing Enter or Escape is a ContentDialog concern (DefaultButton / CloseButtonText), not a button concern.
- Use AccessKey for Alt+key mnemonics and a KeyboardAccelerator for Ctrl+key shortcuts; they are different mechanisms.

## Try this

- Hold Alt and press S — the access key underlines and invokes 'Save'.
- Replace Content with a StackPanel holding a FontIcon and a TextBlock.

## Documentation

- [Microsoft.UI.Xaml.Controls.Button](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.button)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
