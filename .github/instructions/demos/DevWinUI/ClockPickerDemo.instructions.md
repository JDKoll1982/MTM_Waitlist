---
description: "ClockPicker (DevWinUI.ClockPicker) in Controls; a radial clock face for picking a time, as an alternative to the column TimePicker. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/ClockPickerDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/ClockPickerDemo.xaml.cs"
---

# ClockPicker

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.ClockPicker` |
| Page | `DevWinUI/ClockPickerDemo` (`WinUI3LearningLab.Demos.DevWinUI.ClockPickerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo clock-picker` |

## What it teaches

A radial clock face for picking a time, as an alternative to the column TimePicker.

## How the demo is built

The page is a `UserControl` at `DevWinUI/ClockPickerDemo.xaml`, paired with `ClockPickerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `HandoverClock`, `ChosenText`, `ShiftText`.

Wired in XAML: `OnHandoverTimeChanged()`, `OnShiftBEndClick()`, `OnShiftCEndClick()`.

**Code-behind.** Handlers `OnHandoverTimeChanged()`, `OnShiftBEndClick()`, `OnShiftCEndClick()`. 53 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- The selected value is a time-of-day, not a date — pair it with a date control for a full timestamp.
- The face needs a sensible size; below roughly 160 epx the hands become hard to hit with a mouse.
- It raises a typed selected-value-changed event rather than ValueChanged, so the handler signature differs from TimePicker.

## Try this

- Run `WinUI3LearningLab.App.exe --demo clock-picker`. The page shows `ClockPickerDemo.xaml` and `ClockPickerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.ClockPicker](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
