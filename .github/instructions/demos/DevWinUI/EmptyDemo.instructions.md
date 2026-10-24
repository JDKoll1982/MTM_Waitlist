---
description: "Empty (DevWinUI.Empty) in Controls; the designed 'nothing here yet' panel — an icon, a short explanation and an optional action. MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/EmptyDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/EmptyDemo.xaml.cs"
---

# Empty

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.Empty` |
| Page | `DevWinUI/EmptyDemo` (`WinUI3LearningLab.Demos.DevWinUI.EmptyDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo empty-state` |

## What it teaches

The designed 'nothing here yet' panel — an icon, a short explanation and an optional action.

## How the demo is built

The page is a `UserControl` at `DevWinUI/EmptyDemo.xaml`, paired with `EmptyDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 27 element(s).

Named elements the code-behind touches: `StateText`, `LoadingPanel`, `LoadingRing`, `EmptyPanel`, `NothingYet`, `PopulatedPanel`, `Receipts`, `FailureBar`.

Wired in XAML: `OnShowLoadingClick()`, `OnShowEmptyClick()`, `OnShowPopulatedClick()`, `OnPullReceiptsClick()`, `OnShowErrorClick()`.

**Code-behind.** Handlers `OnShowLoadingClick()`, `OnShowEmptyClick()`, `OnShowPopulatedClick()`, `OnPullReceiptsClick()`, `OnShowErrorClick()`. 76 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- An empty state is not an error state: use it for 'no rows match' and reserve InfoBar for failures.
- Show it when the collection is genuinely empty, not while it is still loading — that is what a shimmer is for.
- Always give it an action when the user can resolve the emptiness themselves.

## Try this

- Run `WinUI3LearningLab.App.exe --demo empty-state`. The page shows `EmptyDemo.xaml` and `EmptyDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Empty](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
