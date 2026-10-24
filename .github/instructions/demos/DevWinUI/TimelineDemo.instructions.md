---
description: "Timeline (DevWinUI.Timeline) in Controls; DevWinUI's Timeline is a day band — a horizontal scale with sunrise/sunset shading, not an event list. This page… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/TimelineDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/TimelineDemo.xaml.cs"
---

# Timeline

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.Timeline` |
| Page | `DevWinUI/TimelineDemo` (`WinUI3LearningLab.Demos.DevWinUI.TimelineDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo timeline` |

## What it teaches

DevWinUI's Timeline is a day band — a horizontal scale with sunrise/sunset shading, not an event list. This page shows it doing that, and builds the audit trail you probably wanted from primitives.

## How the demo is built

The page is a `UserControl` at `DevWinUI/TimelineDemo.xaml`, paired with `TimelineDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 19 element(s).

Named elements the code-behind touches: `TrailSummary`, `Trail`, `StatusText`, `DayBand`, `ResourceWarning`.

Wired in XAML: `OnAppendEntryClick()`, `OnAppendOlderEntryClick()`.

**Code-behind.** Handlers `OnAppendEntryClick()`, `OnAppendOlderEntryClick()`. 184 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It has no items and no TimelineItem type: StartTime/EndTime define the band and Sunrise/Sunset shade the daylight portion. Do not expect an audit-trail control.
- Its styles are read from the library's own resource dictionary and the times are set in code, so a purely XAML copy will render empty.
- For an event history, a ListView with a connector column is clearer than bending a day band.

## Try this

- Run `WinUI3LearningLab.App.exe --demo timeline`. The page shows `TimelineDemo.xaml` and `TimelineDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Timeline](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
