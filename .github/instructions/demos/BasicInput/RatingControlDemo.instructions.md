---
description: "RatingControl (Microsoft.UI.Xaml.Controls.RatingControl) in Basic input; star rating with pointer, keyboard and touch support, including a 'no… MTM: MTM_Receiving_Application › Receiving › quick entry."
applyTo: "src/WinUI3LearningLab.App/Demos/BasicInput/RatingControlDemo.xaml,src/WinUI3LearningLab.App/Demos/BasicInput/RatingControlDemo.xaml.cs"
---

# RatingControl

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Basic input |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.RatingControl` |
| Page | `BasicInput/RatingControlDemo` (`WinUI3LearningLab.Demos.BasicInput.RatingControlDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo rating-control` |

## What it teaches

Star rating with pointer, keyboard and touch support, including a 'no rating' state.

## How the demo is built

The page is a `UserControl` at `BasicInput/RatingControlDemo.xaml`, paired with `RatingControlDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 6 element(s).

Named elements the code-behind touches: `Rating`, `StatusText`.

Wired in XAML: `OnRatingChanged()`.

**Code-behind.** Handlers `OnRatingChanged()`. 19 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › quick entry**

The operator scans a part label, types a quantity and posts the receipt. Every action on that screen is a button, a drop-down or a toggle, and the disabled states are the guard that stops a receipt being posted before a dunnage type and quantity are both valid.

Schema this mirrors: `receiving_history, receiving_package_types, dunnage_parts`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Value is a double; a value of -1 means 'unrated' and renders no stars.
- IsClearEnabled lets the user remove the rating by clicking the current value again.
- Caption is a plain string shown under the stars — bind it when you want '4.2 of 5' style feedback.

## Try this

- Run `WinUI3LearningLab.App.exe --demo rating-control`. The page shows `RatingControlDemo.xaml` and `RatingControlDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.RatingControl](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.ratingcontrol)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/BasicInput.cs` or the demo source, then regenerate.
