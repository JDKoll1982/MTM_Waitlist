---
description: "RepeatButton (Microsoft.UI.Xaml.Controls.Primitives.RepeatButton) in Basic input; a button that keeps raising Click while held. It is the… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/RepeatButtonDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/RepeatButtonDemo.xaml.cs"
---

# RepeatButton

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.Primitives.RepeatButton` |
| Page | `BasicInput/RepeatButtonDemo` (`WinUI3LearningLab.Demos.BasicInput.RepeatButtonDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo repeat-button` |

## What it teaches

A button that keeps raising Click while held. It is the primitive behind scrollbar arrows and spin controls.

## How the demo is built

The page is a `UserControl` at `BasicInput/RepeatButtonDemo.xaml`, paired with `RepeatButtonDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `CountText`, `Gauge`.

Wired in XAML: `OnRepeatClicked()`, `OnFillClicked()`, `OnResetClicked()`.

**Code-behind.** Handlers `OnRepeatClicked()`, `OnFillClicked()`, `OnResetClicked()`. 33 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Holding **repeat** re-sends the last label reprint to the same printer — useful when a label jams and the operator reprints several in a row.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Lives in Microsoft.UI.Xaml.Controls.Primitives, so the XAML page needs xmlns:primitives="using:Microsoft.UI.Xaml.Controls.Primitives".
- Delay and Interval are in milliseconds; Interval is the repeat period and defaults to 250 ms.
- Click repeats on a timer, so keep the handler cheap — anything slow will queue up behind the UI thread.

## Try this

- Run `WinUI3LearningLab.App.exe --demo repeat-button`. The page shows `RepeatButtonDemo.xaml` and `RepeatButtonDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.Primitives.RepeatButton](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.repeatbutton)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
