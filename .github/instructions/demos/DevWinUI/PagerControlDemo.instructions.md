---
description: "PagerControl (DevWinUI.PagerControl) in Controls; the numbered page selector WinUI 3 does not ship — first/previous/pages/next/last over a known item count. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/PagerControlDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/PagerControlDemo.xaml.cs"
---

# PagerControl

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Intermediate |
| API | `DevWinUI.PagerControl` |
| Page | `DevWinUI/PagerControlDemo` (`WinUI3LearningLab.Demos.DevWinUI.PagerControlDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo pager-control` |

## What it teaches

The numbered page selector WinUI 3 does not ship — first/previous/pages/next/last over a known item count.

## How the demo is built

The page is a `UserControl` at `DevWinUI/PagerControlDemo.xaml`, paired with `PagerControlDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 13 element(s).

Named elements the code-behind touches: `RangeText`, `RowsHost`, `Pager`.

Wired in XAML: `OnPageChanged()`.

**Code-behind.** Handlers `OnPageChanged()`. 116 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- This is DevWinUI's PagerControl, not a WinUI type. Windows App SDK 2.3.1 has no PagerControl at all, which is why this page exists here rather than under WinUI.
- It computes pages from NumberOfPages and SelectedPageIndex; it does not page your data — you react to the index.
- The page-count text and the numeric buttons are separate display areas; both need to be visible for the control to make sense.

## Try this

- Run `WinUI3LearningLab.App.exe --demo pager-control`. The page shows `PagerControlDemo.xaml` and `PagerControlDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.PagerControl](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
