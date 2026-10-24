---
description: "StorageBar (DevWinUI.StorageBar) in Controls; a capacity bar derived from RangeBase, for showing usage against a limit. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/StorageBarDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/StorageBarDemo.xaml.cs"
---

# StorageBar

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.StorageBar` |
| Page | `DevWinUI/StorageBarDemo` (`WinUI3LearningLab.Demos.DevWinUI.StorageBarDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo storage-bar` |

## What it teaches

A capacity bar derived from RangeBase, for showing usage against a limit.

## How the demo is built

The page is a `UserControl` at `DevWinUI/StorageBarDemo.xaml`, paired with `StorageBarDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 60 element(s).

Named elements the code-behind touches: `BinState`, `OnHandText`, `OnHandBar`, `AllocatedText`, `AllocatedBar`, `AvailableText`, `AvailableBar`, `AllocatedLegend`, `AvailableLegend`, `EmptyLegend`, `QuantitySlider`, `NoteText`.

Wired in XAML: `OnHandText()`, `OnHandBar()`, `OnQuantityChanged()`.

**Code-behind.** Handlers `OnQuantityChanged()`. 64 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- In DevWinUI 10.8.0 it is a single-value RangeBase, not a segmented bar — there are no segments to bind. Showing a split (allocated vs available) means several bars sharing one Maximum.
- It carries no labels, so a legend or tooltip is required for the value to mean anything.
- Do not confuse it with ProgressBar: it is styled for capacity, but the semantics are the same as a progress value.

## Try this

- Run `WinUI3LearningLab.App.exe --demo storage-bar`. The page shows `StorageBarDemo.xaml` and `StorageBarDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.StorageBar](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
