---
description: "LiveGraph (DevWinUI.LiveGraph) in Controls; a continuously scrolling line graph for a live metric such as throughput or temperature. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/LiveGraphDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/LiveGraphDemo.xaml.cs"
---

# LiveGraph

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Advanced |
| API | `DevWinUI.LiveGraph` |
| Page | `DevWinUI/LiveGraphDemo` (`WinUI3LearningLab.Demos.DevWinUI.LiveGraphDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo live-graph` |

## What it teaches

A continuously scrolling line graph for a live metric such as throughput or temperature.

## How the demo is built

The page is a `UserControl` at `DevWinUI/LiveGraphDemo.xaml`, paired with `LiveGraphDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 14 element(s).

Named elements the code-behind touches: `ValueText`, `StreamButton`, `ThroughputGraph`, `StatusText`.

Wired in XAML: `OnStreamToggleClick()`, `OnGraphCreateResources()`, `OnGraphDraw()`.

**Code-behind.** Handlers `OnGraphCreateResources()`, `OnSample()`, `OnGraphDraw()`, `OnStreamToggleClick()`, `OnUnloaded()`. 88 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It is built for a stream, not for a fixed data set; feeding it history then stopping leaves a half-drawn graph.
- The y-axis does not auto-scale beyond the range you configure, so a spike is clipped rather than shown.
- A live graph is inaccessible on its own: pair it with the current value as text.

## Traps recorded in the source

- Ease toward the latest sample so the trace is a curve rather than a staircase; the sample itself still comes from the timer.

## Try this

- Run `WinUI3LearningLab.App.exe --demo live-graph`. The page shows `LiveGraphDemo.xaml` and `LiveGraphDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.LiveGraph](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
