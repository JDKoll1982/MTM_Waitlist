---
description: "NumberBox (Microsoft.UI.Xaml.Controls.NumberBox) in Text; a text box specialised for numbers, with spin buttons, validation, formatting and expression… MTM: MTM_Waitlist › Waitlist › new request."
applyTo: "src/WinUI3LearningLab.App/Demos/Text/NumberBoxDemo.xaml,src/WinUI3LearningLab.App/Demos/Text/NumberBoxDemo.xaml.cs"
---

# NumberBox

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Text |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.NumberBox` |
| Page | `Text/NumberBoxDemo` (`WinUI3LearningLab.Demos.Text.NumberBoxDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo number-box` |

## What it teaches

A text box specialised for numbers, with spin buttons, validation, formatting and expression evaluation.

## How the demo is built

The page is a `UserControl` at `Text/NumberBoxDemo.xaml`, paired with `NumberBoxDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 6 element(s).

Named elements the code-behind touches: `PlainBox`, `ExpressionBox`, `ValidatedBox`, `StatusText`.

Wired in XAML: `OnValueChanged()`.

**Code-behind.** Handlers `OnValueChanged()`. 23 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › new request**

A supervisor types a part number, a quantity and a defect comment, then submits the request. Text input here has to survive a scanner keyboard wedge, a numeric keypad and an operator wearing gloves — which is what the input scopes and the validation states are for.

Quantity is a `double` in the control and a nullable integer in `receiving_history`; the page is where the difference between empty (`NaN`), zero and a real count gets decided.

Schema this mirrors: `waitlist_requests_queue, waitlist_request_item_configs`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Value is a double and NaN means 'empty' — always check double.IsNaN before using it.
- AcceptsExpression allows '2 * 3'; disable it when the field must be exact input.
- ValidationMode decides whether out-of-range input is clamped, invalidated, or disabled — the default is InvalidInputOverwritten.
- ValueChanged does not fire for every keystroke; use it for committed values, not live validation.

## Traps recorded in the source

- Set in code, not XAML: NumberBox.Value fails at parse time when given a literal.

## Try this

- Run `WinUI3LearningLab.App.exe --demo number-box`. The page shows `NumberBoxDemo.xaml` and `NumberBoxDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.NumberBox](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.numberbox)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Text.cs` or the demo source, then regenerate.
