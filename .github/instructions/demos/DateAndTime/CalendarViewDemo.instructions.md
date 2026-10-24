---
description: "CalendarView (Microsoft.UI.Xaml.Controls.CalendarView) in Date & time; a full month grid for date display and selection, including… MTM: MTM_Receiving_Application › Receiving › history filter."
applyTo: "src/WinUI3LearningLab.App/Demos/DateAndTime/CalendarViewDemo.xaml,src/WinUI3LearningLab.App/Demos/DateAndTime/CalendarViewDemo.xaml.cs"
---

# CalendarView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Date & time |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.CalendarView` |
| Page | `DateAndTime/CalendarViewDemo` (`WinUI3LearningLab.Demos.DateAndTime.CalendarViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo calendar-view` |

## What it teaches

A full month grid for date display and selection, including multi-select, ranges and custom day content.

## How the demo is built

The page is a `UserControl` at `DateAndTime/CalendarViewDemo.xaml`, paired with `CalendarViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 9 element(s).

Named elements the code-behind touches: `Schedule`, `SelectionText`.

Wired in XAML: `OnDatesChanged()`, `OnClearSelection()`, `OnBlockThreeDays()`.

**Code-behind.** Handlers `OnDatesChanged()`, `OnClearSelection()`, `OnBlockThreeDays()`. 53 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Receiving_Application › Receiving › history filter**

A clerk narrows a shift's receipts to a date range, then schedules the same range as a recurring report. An empty or partial date is a legitimate state here, so the picker has to represent “no value yet” without pretending to be today.

Schema this mirrors: `receiving_history, reporting_scheduled_reports`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- SelectionMode=Multiple allows SelectedDates to hold more than one entry; SelectedDate is only meaningful for single mode.
- Custom day content comes from CalendarViewDayItemChanging plus the item's Children — this is how you draw density or badges.
- Changing DisplayMode to Year or Decade is how the header back-buttons appear; they are built in.

## Try this

- Run `WinUI3LearningLab.App.exe --demo calendar-view`. The page shows `CalendarViewDemo.xaml` and `CalendarViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.CalendarView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.calendarview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DateAndTime.cs` or the demo source, then regenerate.
