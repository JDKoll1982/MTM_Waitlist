---
description: "Pull to refresh (RefreshContainer) (Microsoft.UI.Xaml.Controls.RefreshContainer) in Collections; wraps a scrollable view and raises RefreshRequested when the… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/RefreshContainerDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/RefreshContainerDemo.xaml.cs"
---

# Pull to refresh (RefreshContainer)

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.RefreshContainer` |
| Page | `Collections/RefreshContainerDemo` (`WinUI3LearningLab.Demos.Collections.RefreshContainerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo pull-to-refresh` |

## What it teaches

Wraps a scrollable view and raises RefreshRequested when the user pulls down past the threshold.

## How the demo is built

The page is a `UserControl` at `Collections/RefreshContainerDemo.xaml`, paired with `RefreshContainerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 25 element(s).

Named elements the code-behind touches: `Host`, `AlertList`, `StateText`.

Wired in XAML: `OnRefreshRequested()`, `OnRequestRefresh()`.

**Code-behind.** Handlers `OnRefreshRequested()`, `OnRequestRefresh()`. 77 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Name the list inside so the container can find it: it needs a ScrollViewer child to observe, otherwise the gesture never triggers.
- The container shows the progress ring automatically; you must call RequestRefreshCompleted when the work finishes.
- PullToRefresh is a touch/pen gesture — test it on a touch device or with the touch emulator.

## Traps recorded in the source

- A mutable class rather than a record: the XAML type info generator emits setters for the public properties of a bound type, and a positional record's init-only members cannot be assigned there (CS8852 inside XamlTypeInfo.g.cs, which reads as an unrelated error).

## Try this

- Run `WinUI3LearningLab.App.exe --demo pull-to-refresh`. The page shows `RefreshContainerDemo.xaml` and `RefreshContainerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.RefreshContainer](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.refreshcontainer)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
