---
description: "ShyHeader (DevWinUI.ShyHeader) in Controls; a header that collapses out of the way as content scrolls and returns when you scroll back. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/ShyHeaderDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/ShyHeaderDemo.xaml.cs"
---

# ShyHeader

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.ShyHeader` |
| Page | `DevWinUI/ShyHeaderDemo` (`WinUI3LearningLab.Demos.DevWinUI.ShyHeaderDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo shy-header` |

## What it teaches

A header that collapses out of the way as content scrolls and returns when you scroll back.

## How the demo is built

The page is a `UserControl` at `DevWinUI/ShyHeaderDemo.xaml`, paired with `ShyHeaderDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 15 element(s).

Named elements the code-behind touches: `LineHost`.

**Code-behind.** 57 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It needs a scroll source to watch; without one it simply stays expanded.
- Collapsing a header that carries the only navigation affordance strands the user mid-scroll.
- Test with keyboard focus: if focus moves into the hidden header, scrolling must reveal it again.

## Try this

- Run `WinUI3LearningLab.App.exe --demo shy-header`. The page shows `ShyHeaderDemo.xaml` and `ShyHeaderDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.ShyHeader](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
