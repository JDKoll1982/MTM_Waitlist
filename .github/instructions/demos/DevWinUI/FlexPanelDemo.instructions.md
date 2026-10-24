---
description: "FlexPanel (DevWinUI.FlexPanel) in Controls; a flexbox-style layout panel with grow, shrink and basis per child — CSS flex semantics inside XAML. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/FlexPanelDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/FlexPanelDemo.xaml.cs"
---

# FlexPanel

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.FlexPanel` |
| Page | `DevWinUI/FlexPanelDemo` (`WinUI3LearningLab.Demos.DevWinUI.FlexPanelDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo flex-panel` |

## What it teaches

A flexbox-style layout panel with grow, shrink and basis per child — CSS flex semantics inside XAML.

## How the demo is built

The page is a `UserControl` at `DevWinUI/FlexPanelDemo.xaml`, paired with `FlexPanelDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 29 element(s).

Named elements the code-behind touches: `DockGrow`, `StagingGrow`, `HoldGrow`, `DockPanel`, `DockInfo`, `StagingPanel`, `StagingInfo`, `HoldPanel`, `HoldInfo`, `WidthText`.

Wired in XAML: `OnGrowChanged()`.

**Code-behind.** Handlers `OnGrowChanged()`. 42 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- FlexPanel is DevWinUI's; WinUI has no built-in LayoutPanel, so copy this rather than looking for a native one.
- Grow and shrink are weights, not sizes: a child with Grow=2 takes twice the free space, not twice the width.
- It does not virtualise; large child counts need ItemsRepeater with a layout instead.

## Try this

- Run `WinUI3LearningLab.App.exe --demo flex-panel`. The page shows `FlexPanelDemo.xaml` and `FlexPanelDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.FlexPanel](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
