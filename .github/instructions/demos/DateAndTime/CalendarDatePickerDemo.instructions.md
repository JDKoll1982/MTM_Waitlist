---
description: "CalendarDatePicker (Microsoft.UI.Xaml.Controls.CalendarDatePicker) in Date & time; a text field plus calendar flyout: type a date or pick one… MTM: MTM_Receiving_Application › Receiving › history filter."
applyTo: "src/WinUI3LearningLab.App/Demos/DateAndTime/CalendarDatePickerDemo.xaml,src/WinUI3LearningLab.App/Demos/DateAndTime/CalendarDatePickerDemo.xaml.cs"
---

# CalendarDatePicker

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Date & time |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.CalendarDatePicker` |
| Page | `DateAndTime/CalendarDatePickerDemo` (`WinUI3LearningLab.Demos.DateAndTime.CalendarDatePickerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo calendar-date-picker` |

## What it teaches

A text field plus calendar flyout: type a date or pick one. Good when a date matters but the calendar is secondary.

## How the demo is built

The page is a `UserControl` at `DateAndTime/CalendarDatePickerDemo.xaml`, paired with `CalendarDatePickerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 9 element(s).

Named elements the code-behind touches: `DeliveryPicker`, `ValueText`.

Wired in XAML: `OnDateChanged()`, `OnTodayClick()`, `OnClearClick()`.

**Code-behind.** Handlers `OnDateChanged()`, `OnTodayClick()`, `OnClearClick()`. 50 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › history filter**

A clerk narrows a shift's receipts to a date range, then schedules the same range as a recurring report. An empty or partial date is a legitimate state here, so the picker has to represent “no value yet” without pretending to be today.

The history filter accepts an empty date to mean “all shifts”, which is why the picker is nullable where `DatePicker` is not.

Schema this mirrors: `receiving_history, reporting_scheduled_reports`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Date is DateTimeOffset? — null means 'not chosen', which is different from today.
- DateChanged fires for both typed and picked values; there is no separate 'validated' event.
- MinDate/MaxDate default to a wide range but do not block typed text until focus leaves the control.

## Try this

- Run `WinUI3LearningLab.App.exe --demo calendar-date-picker`. The page shows `CalendarDatePickerDemo.xaml` and `CalendarDatePickerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.CalendarDatePicker](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.calendardatepicker)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DateAndTime.cs` or the demo source, then regenerate.
