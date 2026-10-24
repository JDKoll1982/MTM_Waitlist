---
description: "Shimmer (DevWinUI.Shimmer) in Controls; the skeleton/shimmer placeholder shown while real data loads, instead of an indefinite ProgressRing over an empty… MTM: MTM_Waitlist › Operations dashboard."
applyTo: "src/WinUI3LearningLab.App/Demos/DevWinUI/ShimmerDemo.xaml,src/WinUI3LearningLab.App/Demos/DevWinUI/ShimmerDemo.xaml.cs"
---

# Shimmer

| | |
| --- | --- |
| Ecosystem | DevWinUI |
| Category | Controls |
| Level | Starter |
| API | `DevWinUI.Shimmer` |
| Page | `DevWinUI/ShimmerDemo` (`WinUI3LearningLab.Demos.DevWinUI.ShimmerDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo shimmer` |

## What it teaches

The skeleton/shimmer placeholder shown while real data loads, instead of an indefinite ProgressRing over an empty list.

## How the demo is built

The page is a `UserControl` at `DevWinUI/ShimmerDemo.xaml`, paired with `ShimmerDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 42 element(s).

Named elements the code-behind touches: `LoadButton`, `StatusText`, `Skeleton`, `Rows`.

Wired in XAML: `OnLoadClick()`.

**Code-behind.** Handlers `OnLoadClick()`. 75 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Operations dashboard**

The dashboard steps through a job, shows a PIN challenge before a supervisor override, and keeps widgets alive while tables load. These are the composite controls that the real apps would otherwise hand-roll for every screen.

The board loads `waitlist_requests_queue` over a slow shop-floor link; the shimmer placeholder keeps the layout stable so the first paint does not jump when the rows arrive.

Schema this mirrors: `setup_active_jobs, waitlist_requests_queue, auth_user_management_audit`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- Shimmer animates only while it is visible and loaded; toggling Visibility is the intended switch, not starting a storyboard.
- It is a placeholder, not a loader — it conveys the shape of the content that is coming. A shimmer block that does not match the eventual row layout looks like a bug.
- ShimmerPrimitives are the individual blocks; their size is yours to set, and they do not inherit from a parent template.

## Try this

- Run `WinUI3LearningLab.App.exe --demo shimmer`. The page shows `ShimmerDemo.xaml` and `ShimmerDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [DevWinUI.Shimmer](https://github.com/ghost1372/DevWinUI)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/DevWinUI.cs` or the demo source, then regenerate.
