---
description: "FlipView (Microsoft.UI.Xaml.Controls.FlipView) in Collections; a one-item-at-a-time pager with swipe, arrows and keyboard support — a carousel… MTM: MTM_Waitlist › Waitlist › queue and lugger board."
applyTo: "src/WinUI3LearningLab.App/Demos/Collections/FlipViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Collections/FlipViewDemo.xaml.cs"
---

# FlipView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Collections |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.FlipView` |
| Page | `Collections/FlipViewDemo` (`WinUI3LearningLab.Demos.Collections.FlipViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo flip-view` |

## What it teaches

A one-item-at-a-time pager with swipe, arrows and keyboard support — a carousel with selection built in.

## How the demo is built

The page is a `UserControl` at `Collections/FlipViewDemo.xaml`, paired with `FlipViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 34 element(s).

Named elements the code-behind touches: `RiskFlip`, `PositionText`.

Wired in XAML: `OnFlipSelectionChanged()`, `OnNextClick()`.

**Code-behind.** Handlers `OnFlipSelectionChanged()`, `OnNextClick()`. 78 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_Waitlist › Waitlist › queue and lugger board**

The queue is a live list of requests with a status, an age and an assigned lugger. Virtualisation and incremental loading matter because the board is left open for a whole shift and grows while the operator is not looking at it.

Schema this mirrors: `waitlist_requests_queue, waitlist_lugger_locations`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- It virtualises like ListView, so only a couple of items exist at once — do not reach into the container of an off-screen item.
- FlipView does not loop; wrap-around needs code that moves SelectedIndex yourself.
- Set UseTouchAnimationsForAllNavigation to get the swipe animation on mouse and keyboard too.

## Try this

- Run `WinUI3LearningLab.App.exe --demo flip-view`. The page shows `FlipViewDemo.xaml` and `FlipViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.FlipView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.flipview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Collections.cs` or the demo source, then regenerate.
