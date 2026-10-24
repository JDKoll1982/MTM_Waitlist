---
description: "TimePicker (Microsoft.UI.Xaml.Controls.TimePicker) in Date & time; two or three columns for hours, minutes and optionally seconds, in 12-… MTM: MTM_Receiving_Application › Receiving › history filter."
applyTo: "src/WinUI3LearningLab.App/Demos/DateAndTime/TimePickerDemo.xaml,src/WinUI3LearningLab.App/Demos/DateAndTime/TimePickerDemo.xaml.cs"
---

# TimePicker

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Date & time |
| Level | Starter |
| API | `Microsoft.UI.Xaml.Controls.TimePicker` |
| Page | `DateAndTime/TimePickerDemo` (`WinUI3LearningLab.Demos.DateAndTime.TimePickerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo time-picker` |

## What it teaches

Two or three columns for hours, minutes and optionally seconds, in 12- or 24-hour form.

## How the demo is built

The page is a `UserControl` at `DateAndTime/TimePickerDemo.xaml`, paired with `TimePickerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 9 element(s).

Named elements the code-behind touches: `ClockBox`, `ShiftPicker`, `ValueText`.

Wired in XAML: `OnClockChanged()`, `OnTimeChanged()`.

**Code-behind.** Handlers `OnClockChanged()`, `OnTimeChanged()`. 39 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › history filter**

A clerk narrows a shift's receipts to a date range, then schedules the same range as a recurring report. An empty or partial date is a legitimate state here, so the picker has to represent “no value yet” without pretending to be today.

Schema this mirrors: `receiving_history, reporting_scheduled_reports`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ClockIdentifier defaults to 12-hour; pass "24HourClock" for 24-hour display.
- Time is a TimeSpan measured from midnight, not a DateTime.
- MinuteIncrement changes the step; setting it to 1 with seconds hidden still lets the value carry seconds you did not type.

## Try this

- Run `WinUI3LearningLab.App.exe --demo time-picker`. The page shows `TimePickerDemo.xaml` and `TimePickerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.TimePicker](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.timepicker)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DateAndTime.cs` or the demo source, then regenerate.
