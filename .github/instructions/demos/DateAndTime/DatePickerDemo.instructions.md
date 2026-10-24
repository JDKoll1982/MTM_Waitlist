---
description: "DatePicker (Microsoft.UI.Xaml.Controls.DatePicker) in Date & time; three spinning columns for year, month and day. It never returns null… MTM: MTM_Receiving_Application › Receiving › history filter."
applyTo: "src/WinUI3LearningLab.App/Demos/DateAndTime/DatePickerDemo.xaml,src/WinUI3LearningLab.App/Demos/DateAndTime/DatePickerDemo.xaml.cs"
---

# DatePicker

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Date & time |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.DatePicker` |
| Page | `DateAndTime/DatePickerDemo` (`WinUI3LearningLab.Demos.DateAndTime.DatePickerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo date-picker` |

## What it teaches

Three spinning columns for year, month and day. It never returns null, so it is for required dates.

## How the demo is built

The page is a `UserControl` at `DateAndTime/DatePickerDemo.xaml`, paired with `DatePickerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 10 element(s).

Named elements the code-behind touches: `ShipPicker`, `DayToggle`, `MonthToggle`, `ValueText`.

Wired in XAML: `OnDateChanged()`, `OnColumnToggle()`, `OnClearSelectedDate()`.

**Code-behind.** Handlers `OnDateChanged()`, `OnColumnToggle()`, `OnClearSelectedDate()`. 59 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › history filter**

A clerk narrows a shift's receipts to a date range, then schedules the same range as a recurring report. An empty or partial date is a legitimate state here, so the picker has to represent “no value yet” without pretending to be today.

Schema this mirrors: `receiving_history, reporting_scheduled_reports`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Date is a DateTimeOffset and defaults to the current date — there is no empty state, unlike CalendarDatePicker.
- SelectedDate is nullable internally; clearing it re-selects today rather than showing a blank picker.
- Use DayFormat/MonthFormat to control the column text; DayVisible and friends hide columns for month-only input.

## Try this

- Run `WinUI3LearningLab.App.exe --demo date-picker`. The page shows `DatePickerDemo.xaml` and `DatePickerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.DatePicker](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.datepicker)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DateAndTime.cs` or the demo source, then regenerate.
