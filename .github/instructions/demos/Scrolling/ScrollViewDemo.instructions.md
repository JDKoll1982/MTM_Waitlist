---
description: "ScrollView (Microsoft.UI.Xaml.Controls.ScrollView) in Scrolling; the modern scrolling surface: one child, composable… MTM: MTM_WIP_Application_WinForms › Inventory › stock list and last transactions."
applyTo: "src/WinUI3LearningLab.App/Demos/Scrolling/ScrollViewDemo.xaml,src/WinUI3LearningLab.App/Demos/Scrolling/ScrollViewDemo.xaml.cs"
---

# ScrollView

| | |
| --- | --- |
| Ecosystem | WinUI 3 |
| Category | Scrolling |
| Level | Intermediate |
| API | `Microsoft.UI.Xaml.Controls.ScrollView` |
| Page | `Scrolling/ScrollViewDemo` (`WinUI3LearningLab.Demos.Scrolling.ScrollViewDemo`) |
| Kind | Live, interactive demo |
| Run it | `WinUI3LearningLab.App.exe --demo scroll-view` |

## What it teaches

The modern scrolling surface: one child, composable scrollbar and zoom behaviour, animation-driven.

## How the demo is built

The page is a `UserControl` at `Scrolling/ScrollViewDemo.xaml`, paired with `ScrollViewDemo.xaml.cs`. The shell hosts it inside `Shell/DemoPage.xaml`, which also shows both files verbatim — the snippet viewer reads the same source the app compiles, so what is displayed is always what runs.

**Layout.** Root `UserControl` with 44 element(s).

Named elements the code-behind touches: `ZoomSlider`, `ZoomText`, `DemoScroll`, `ScrollContent`, `ShippingSection`, `ScrollStatus`.

Wired in XAML: `OnScrollTopClicked()`, `OnScrollByClicked()`, `OnScrollBottomClicked()`, `OnJumpClicked()`, `OnZoomChanged()`, `OnViewChanged()`.

**Code-behind.** Handlers `OnScrollTopClicked()`, `OnScrollByClicked()`, `OnScrollBottomClicked()`, `OnJumpClicked()`, `OnZoomChanged()`, `OnViewChanged()`. 46 line(s), including the comments that record what the control does that the docs do not say.

Anything the demo cannot show is explained on the page rather than faked: the same rules the rest of the repo follows are in `.github/instructions/winui3-api-rules.instructions.md`.

## MTM use case

**MTM_WIP_Application_WinForms › Inventory › stock list and last transactions**

The stock list is tens of thousands of rows on a workstation that also runs the label printer. Realising rows lazily and keeping the header in view is the difference between a usable screen and a frozen one.

Schema this mirrors: `inv_inventory, sys_last_10_transactions`. Sample data for the page comes from `src/WinUI3LearningLab.Core/LabData.cs` — part numbers, bins, dunnage types and work orders, never `Item 1` or `John Doe`.

## Gotchas

- ScrollView takes exactly one child; wrap content in a StackPanel or Grid.
- ScrollViewer (below) still exists and is what most templates use — ScrollView is the newer, more controllable replacement.
- ScrollingAnimationMode and the content-driven scrollbar visibility are the parts ScrollViewer does not expose.

## Try this

- Run `WinUI3LearningLab.App.exe --demo scroll-view`. The page shows `ScrollViewDemo.xaml` and `ScrollViewDemo.xaml.cs` beside the live demo, so the next edit can be made against something already running.
- Change one value in the XAML and rebuild; the snippet viewer re-reads the file, so what is shown stays the code that compiled.

## Documentation

- [Microsoft.UI.Xaml.Controls.ScrollView](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.scrollview)
- [WinUI 3 API rules for this repo](../../winui3-api-rules.instructions.md)
- [Design system](../../winui3-design-system.instructions.md)

---

Generated from `DemoCatalog` by `--export-instructions`. Do not edit by hand: change the descriptor in `src/WinUI3LearningLab.Core/Catalog/Scrolling.cs` or the demo source, then regenerate.
